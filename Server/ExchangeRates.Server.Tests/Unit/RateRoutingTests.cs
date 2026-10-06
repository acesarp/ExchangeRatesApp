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
/// How the service picks a source for a pair: falling back between banks, and crossing through a bank's native currency
/// when no bank quotes the pair directly. Uses the Czech National Bank provider against a stubbed HTTP API.
/// </summary>
public class RateRoutingTests {
	private static readonly DateOnly Day = new(2026, 10, 1);

	private static readonly CurrencyEntity Usd = new("USD", 840, "US Dollar", false, 1) { Id = 1 };
	private static readonly CurrencyEntity Eur = new("EUR", 978, "Euro", false, 2) { Id = 2 };
	private static readonly CurrencyEntity Czk = new("CZK", 203, "Czech Koruna", false, 29) { Id = 29 };

	private const string WorkingUrl = "https://cnb.test/rates";
	private const string BrokenUrl = "https://cnb-down.test/rates";

	// CNB quotes CZK per foreign unit: 1 USD = 21.80 CZK, 1 EUR = 24.50 CZK.
	private const string CnbResponse = """
		{ "rates": [
			{ "validFor": "2026-10-01", "currencyCode": "USD", "amount": 1, "rate": 21.80 },
			{ "validFor": "2026-10-01", "currencyCode": "EUR", "amount": 1, "rate": 24.50 }
		] }
		""";

	[Fact]
	public async Task GetRatesAsync_FirstBankFails_FallsBackToNextBank() {
		var broken = CnbBank(id: 7, BrokenUrl);
		var working = CnbBank(id: 8, WorkingUrl);
		var (service, repository, saved) = CreateService([broken, working]);

		var rates = await service.GetRatesAsync("USD", "CZK", Day, Day, CancellationToken.None);

		rates.Should().ContainSingle();
		rates[0].Rate.Should().BeApproximately(21.80m, 0.0000001m);
		rates[0].Provider.Should().Be("CNB");
		saved.Should().ContainSingle();
		repository.Verify(x => x.AddUnavailableDatesAsync(It.Is<IEnumerable<ExchangeRateUnavailableDateEntity>>(d => d.All(u => u.CentralBankId == working.Id)), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetRatesAsync_NoBankQuotesPairDirectly_CrossesThroughBankNativeCurrency() {
		// The only bank publishing both USD and EUR is CNB, whose native currency is CZK.
		var (service, _, saved) = CreateService([CnbBank(id: 7, WorkingUrl)]);

		var rates = await service.GetRatesAsync("USD", "EUR", Day, Day, CancellationToken.None);

		// USD/EUR = USD/CZK * CZK/EUR = 21.80 / 24.50
		rates.Should().ContainSingle();
		rates[0].BaseCurrency.Should().Be("USD");
		rates[0].QuoteCurrency.Should().Be("EUR");
		rates[0].Rate.Should().BeApproximately(21.80m / 24.50m, 0.0000001m);
		rates[0].Provider.Should().Be("CNB");

		// Both legs are real bank rates and are stored for later requests.
		saved.Select(x => (x.BaseCurrencyId, x.QuoteCurrencyId)).Should().BeEquivalentTo([(Usd.Id, Czk.Id), (Eur.Id, Czk.Id)]);
	}

	[Fact]
	public async Task GetRatesAsync_BankWithoutProviderImplementation_IsSkipped() {
		var unimplemented = CnbBank(id: 6, WorkingUrl);
		unimplemented.BankCode = "NOPROVIDER";
		var (service, _, _) = CreateService([unimplemented, CnbBank(id: 7, WorkingUrl)]);

		var rates = await service.GetRatesAsync("USD", "CZK", Day, Day, CancellationToken.None);

		rates.Should().ContainSingle().Which.Provider.Should().Be("CNB");
	}

	private static CentralBankEntity CnbBank(int id, string apiUrl) {
		var bank = new CentralBankEntity {
			Id = id,
			BankCode = "CNB",
			BankName = "Czech National Bank",
			ApiUrl = apiUrl,
			NativeCurrency = Czk,
			CurrencyId = Czk.Id,
			IsActive = true
		};
		bank.SupportedCurrencies.Add(new CentralBankSupportedCurrencyEntity { Currency = Usd, CurrencyId = Usd.Id, CentralBank = bank });
		bank.SupportedCurrencies.Add(new CentralBankSupportedCurrencyEntity { Currency = Eur, CurrencyId = Eur.Id, CentralBank = bank });
		return bank;
	}

	private static (ExchangeRateService Service, Mock<IExchangeRateRepository> Repository, List<ExchangeRateEntity> Saved) CreateService(IReadOnlyList<CentralBankEntity> banks) {
		var saved = new List<ExchangeRateEntity>();
		var currencies = new[] { Usd, Eur, Czk };

		var repository = new Mock<IExchangeRateRepository>();
		repository.Setup(x => x.GetRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		repository.Setup(x => x.GetUnavailableDatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		repository.Setup(x => x.GetBanksPublishingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(banks);
		repository.Setup(x => x.GetCurrencyByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((IEnumerable<string> codes, CancellationToken _) => currencies.Where(c => codes.Contains(c.CurrencyCode)).ToList());
		repository.Setup(x => x.AddUnavailableDatesAsync(It.IsAny<IEnumerable<ExchangeRateUnavailableDateEntity>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(0);
		repository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<ExchangeRateEntity>>(), It.IsAny<CancellationToken>()))
				.Callback((IEnumerable<ExchangeRateEntity> rates, CancellationToken _) => saved.AddRange(rates))
				.ReturnsAsync(1);

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["PivotCurrency"] = "USD" })
			.Build();

		var services = new ServiceCollection()
			.AddSingleton<IConfiguration>(configuration)
			.AddSingleton(new HttpClient(new StubHandler()))
			.AddLogging()
			.BuildServiceProvider();

		var factory = new CentralBankProviderFactory(services, NullLogger<CentralBankProviderFactory>.Instance);
		var service = new ExchangeRateService(NullLogger<ExchangeRateService>.Instance, repository.Object, factory, configuration);

		return (service, repository, saved);
	}

	private sealed class StubHandler : HttpMessageHandler {
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
			var response = request.RequestUri!.Host == new Uri(WorkingUrl).Host
				? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(CnbResponse, Encoding.UTF8, "application/json") }
				: new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
			return Task.FromResult(response);
		}
	}
}
