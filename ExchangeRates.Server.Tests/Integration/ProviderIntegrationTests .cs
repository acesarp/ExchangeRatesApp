using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Providers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

using Xunit;

namespace ExchangeRates.Server.Tests.Integration;

public sealed class ProviderIntegrationTests {
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_ForDateRange() {
		Debugger.Break();

		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();

		using var http = new HttpClient();

		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOEProvider>();

		var provider = new BOEProvider(http, configuration, logger);

		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);

		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);

		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.GBP, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BOE", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
}