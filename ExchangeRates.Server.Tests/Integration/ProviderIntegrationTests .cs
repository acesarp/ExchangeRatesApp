using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Providers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

using Xunit;

namespace ExchangeRates.Server.Tests.Integration;

/// <summary>
/// Integration tests for the exchange rate providers.
/// </summary>
public sealed class ProviderIntegrationTests {
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_ForDateRange() {
		Debugger.Break();

		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.AddJsonFile("providerkeys.json")
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

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_EurRates_FromFRED_ForDateRange() {
		Debugger.Break();

		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.AddJsonFile("providerkeys.json")
			.Build();

		using var http = new HttpClient();

		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<FREDProvider>();

		var provider = new FREDProvider(http, configuration, logger);

		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);

		var rates = await provider.GetRatesAsync(ECurrencyISO.EUR, fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);

		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.USD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.EUR, rate.QuoteCurrency);
			Assert.Equal("FRED", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_EurRates_FromBCB_ForDateRange() {
		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.Build();

		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCBProvider>();
		using var http = new HttpClient();

		var provider = new BCBProvider(http, configuration, logger);

		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);

		var rates = await provider.GetRatesAsync(ECurrencyISO.EUR, fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);

		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EUR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.BRL, rate.QuoteCurrency);
			Assert.Equal("BCB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBDI_ForDateRange() {
		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.AddJsonFile("providerkeys.json")
			.Build();

		using var http = new HttpClient();

		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BDIProvider>();

		var provider = new BDIProvider(http, configuration, logger);

		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);

		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);

		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EUR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BDI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_BrlRates_FromBOI_ForDateRange() {
		var configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.AddJsonFile("providerkeys.json")
			.Build();

		using var http = new HttpClient();

		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOIProvider>();

		var provider = new BOIProvider(http, configuration, logger);

		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);

		var rates = await provider.GetRatesAsync(ECurrencyISO.BRL, fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);

		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.ILS, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.BRL, rate.QuoteCurrency);
			Assert.Equal("BOI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
}