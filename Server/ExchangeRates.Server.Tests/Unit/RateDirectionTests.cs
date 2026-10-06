using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Infrastructure.Providers;
using ExchangeRates.Server.Services;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using System.Net;
using System.Text;

using Xunit;

namespace ExchangeRates.Server.Tests.Unit;

/// <summary>
/// Rates must flow through the system as "1 unit of base = Rate units of quote", whichever way the source publishes them.
/// Uses the Czech National Bank, which publishes CZK per foreign unit (1 USD = 21.80 CZK).
/// </summary>
public class RateDirectionTests {
	private static readonly DateOnly Day = new(2026, 10, 1);
	private const decimal CzkPerUsd = 21.80m;

	private static readonly CurrencyEntity Usd = new("USD", 840, "US Dollar", false, 1) { Id = 1 };
	private static readonly CurrencyEntity Czk = new("CZK", 203, "Czech Koruna", false, 29) { Id = 29 };

	private const string CnbResponse = """	{ "rates": [{ "validFor": "2026-10-01", "currencyCode": "USD", "amount": 1, "rate": 21.80 }] }""";

	[Fact]
	public async Task Provider_PublishingNativePerForeign_ReturnsForeignPerNative() {
		var provider = CreateCnbProvider();

		var rates = await provider.GetRatesAsync("USD", Day, Day, CancellationToken.None);

		rates.Should().ContainSingle();
		rates[0].Rate.Should().BeApproximately(1m / CzkPerUsd, 0.0000001m);
		rates[0].Provider.Should().Be("CNB");
	}

	[Theory]
	[InlineData("USD", "CZK", 21.80)]
	[InlineData("CZK", "USD", 1 / 21.80)]
	public async Task Service_ReturnsRateInRequestedDirection(string baseCurrency, string quoteCurrency, double expected) {
		var (service, _) = CreateService();

		var rates = await service.GetRatesAsync(baseCurrency, quoteCurrency, Day, Day, CancellationToken.None);

		rates.Should().ContainSingle();
		rates[0].BaseCurrency.Should().Be(baseCurrency);
		rates[0].QuoteCurrency.Should().Be(quoteCurrency);
		rates[0].Rate.Should().BeApproximately((decimal)expected, 0.0000001m);
	}

	[Theory]
	[InlineData("USD", "CZK")]
	[InlineData("CZK", "USD")]
	public async Task Service_StoresRateUnderCanonicalCurrencies_RegardlessOfRequestDirection(string baseCurrency, string quoteCurrency) {
		var (service, saved) = CreateService();

		await service.GetRatesAsync(baseCurrency, quoteCurrency, Day, Day, CancellationToken.None);

		// USD has the higher priority, so the canonical row is USD/CZK = 21.80 for either request direction.
		saved.Should().ContainSingle();
		saved[0].BaseCurrencyId.Should().Be(Usd.Id);
		saved[0].QuoteCurrencyId.Should().Be(Czk.Id);
		saved[0].Rate.Should().BeApproximately(CzkPerUsd, 0.0000001m);
	}

	private static CentralBankEntity CnbBank() {
		var bank = new CentralBankEntity {
			Id = 7,
			BankCode = "CNB",
			BankName = "Czech National Bank",
			ApiUrl = "https://cnb.test/rates",
			NativeCurrency = Czk,
			CurrencyId = Czk.Id
		};
		bank.SupportedCurrencies.Add(new CentralBankSupportedCurrencyEntity { Currency = Usd, CurrencyId = Usd.Id, CentralBank = bank });
		return bank;
	}

	private static HttpClient StubHttp() => new(new StubHandler(CnbResponse));

	private static CNBProvider CreateCnbProvider() =>
		new(StubHttp(), new ConfigurationBuilder().Build(), CnbBank(), NullLogger<CNBProvider>.Instance);

	private static (ExchangeRateService Service, List<ExchangeRateEntity> Saved) CreateService() {
		var bank = CnbBank();
		var saved = new List<ExchangeRateEntity>();

		var repository = new Mock<IExchangeRateRepository>();
		repository.Setup(x => x.GetRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		repository.Setup(x => x.GetUnavailableDatesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		repository.Setup(x => x.FindSuitableBankAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>()))
				.ReturnsAsync(bank);
		repository.Setup(x => x.GetCentralBankAsync("CNB", It.IsAny<CancellationToken>()))
				.ReturnsAsync(bank);
		repository.Setup(x => x.GetCurrencyByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((IEnumerable<string> codes, CancellationToken _) => new[] { Usd, Czk }.Where(c => codes.Contains(c.CurrencyCode)).ToList());
		repository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<ExchangeRateEntity>>(), It.IsAny<CancellationToken>()))
				.Callback((IEnumerable<ExchangeRateEntity> rates, CancellationToken _) => saved.AddRange(rates))
				.ReturnsAsync(1);

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["PivotCurrency"] = "USD" })
			.Build();

		var services = new ServiceCollection()
			.AddSingleton<IConfiguration>(configuration)
			.AddSingleton(StubHttp())
			.AddLogging()
			.BuildServiceProvider();

		var factory = new CentralBankProviderFactory(services, NullLogger<CentralBankProviderFactory>.Instance);
		var service = new ExchangeRateService(NullLogger<ExchangeRateService>.Instance, repository.Object, factory, configuration);

		return (service, saved);
	}

	private sealed class StubHandler(string body) : HttpMessageHandler {
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
	}
}
