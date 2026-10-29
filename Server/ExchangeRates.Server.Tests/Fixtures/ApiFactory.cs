using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Server.Workers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

namespace ExchangeRates.Server.Tests.Fixtures;

/// <summary>
/// Boots the real API pipeline with the repository mocked, so controller tests need no database,
/// no central bank APIs and no background workers.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program> {
	public static readonly CurrencyEntity Usd = new("USD", 840, "US Dollar", false, 1) { Id = 1 };
	public static readonly CurrencyEntity Eur = new("EUR", 978, "Euro", false, 2) { Id = 2 };

	public Mock<IExchangeRateRepository> Repository { get; } = new();

	public ApiFactory() {
		Repository.Setup(x => x.GetRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		Repository.Setup(x => x.GetUnavailableDatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		Repository.Setup(x => x.GetCurrenciesAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync([Usd, Eur]);
		Repository.Setup(x => x.GetFixedRatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		Repository.Setup(x => x.GetBanksPublishingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
	}

	/// <summary> Makes the repository return a cached USD/EUR rate for the given day </summary>
	public void CacheUsdEurRate(DateOnly date, decimal rate) {
		var entity = new ExchangeRateEntity {
			Date = date,
			BaseCurrency = Usd,
			BaseCurrencyId = Usd.Id,
			QuoteCurrency = Eur,
			QuoteCurrencyId = Eur.Id,
			Rate = rate,
			Provider = "TEST"
		};

		Repository.Setup(x => x.GetRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([entity]);
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder) {
		// UseSetting is visible while Program builds, so an empty connection string keeps the SQL log sink off.
		builder.UseSetting("ConnectionStrings:ExchangeRates", "");
		builder.UseSetting("IdentityServer:Authority", "https://login.microsoftonline.com/test-tenant/v2.0");
		builder.UseSetting("IdentityServer:ClientId", "test-client");
		builder.UseSetting("IdentityServer:ClientSecret", "test-secret");

		builder.ConfigureServices(services => {
			services.RemoveAll<IExchangeRateRepository>();
			services.AddSingleton(Repository.Object);

			var workers = services.Where(d => d.ImplementationType == typeof(BitcoinPriceWorker) || d.ImplementationType == typeof(HealthStatusWorker)).ToList();
			foreach (var worker in workers) {
				services.Remove(worker);
			}
		});
	}
}
