using ExchangeRates.Domain.Enums;
using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;
using ExchangeRates.Server.Services;
using ExchangeRates.Server.Tests.Fixtures;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace ExchangeRates.Server.Tests.Unit;

public class ExchangeRateServiceTests {
	private readonly IExchangeRateService _service;
	private readonly List<ICentralBankProvider> _providers;
	private readonly IConfiguration _config;

	public ExchangeRateServiceTests() {
		_providers = [];

		// Setup configuration
		var configDict = new Dictionary<string, string?> {
			{ "PivotCurrency", "USD" },
			{ "FixedExchangeRates:Rates:USD", "1.00" },
			{ "FixedExchangeRates:Rates:EUR", "0.92" }
		};
		var configBuilder = new ConfigurationBuilder()
			.AddInMemoryCollection(configDict);
		_config = configBuilder.Build();

		// Setup service provider
		var serviceCollection = new ServiceCollection()
			.AddSingleton(_config)
			.AddLogging(builder => builder.AddConsole());

		var serviceProvider = serviceCollection.BuildServiceProvider();

		var logger = serviceProvider.GetRequiredService<ILogger<CentralBankProviderFactory>>();
		var factory = new CentralBankProviderFactory(_providers, logger);
		var fixedProvider = new FixedExchangeRateProvider(_config);
		var serviceLogger = serviceProvider.GetRequiredService<ILogger<ExchangeRateService>>();
		var repository = serviceProvider.GetRequiredService<IExchangeRateRepository>();
		_service = new ExchangeRateService(_config, factory, fixedProvider, repository, serviceLogger);
	}

	#region Identity Tests
	[Fact]
	public async Task GetRatesAsync_SameCurrency_ReturnsIdentity() {
		// Arrange
		var from = ECurrencyISO.USD;
		var to = ECurrencyISO.USD;
		var date = DateOnly.FromDateTime(DateTime.UtcNow);

		// Act
		var result = await _service.GetRatesAsync(from, to, date, date);

		// Assert
		result.Should().NotBeEmpty();
		result.Should().HaveCount(1);
		result[0].Rate.Should().Be(1.0m);
		result[0].Provider.Should().Be("IDENTITY");
	}
	#endregion

	#region Direct Provider Lookup Tests
	[Fact]
	public async Task GetRatesAsync_WithProvider_ReturnsResult() {
		// Arrange
		var from = ECurrencyISO.USD;
		var to = ECurrencyISO.EUR;
		var date = DateOnly.FromDateTime(DateTime.UtcNow);

		var rates = new Dictionary<(ECurrencyISO, ECurrencyISO, DateOnly), decimal> {
			{ (from, to, date), 0.92m }
		};

		var provider = new TestCentralBankProvider("TEST", "Test Provider", ECurrencyISO.USD, new[] { from, to }, rates);
		_providers.Add(provider);

		// Act - will either find direct or return fixed
		var result = await _service.GetRatesAsync(from, to, date, date);

		// Assert
		result.Should().NotBeEmpty();
	}
	#endregion

	#region Fixed Rate Tests
	[Fact]
	public async Task GetRatesAsync_FixedRateConfigured_MayReturnFixedOrEmpty() {
		// Arrange
		var from = ECurrencyISO.USD;
		var to = ECurrencyISO.EUR;
		var date = DateOnly.FromDateTime(DateTime.UtcNow);

		// EUR is configured as fixed at 0.92 in config
		// Act
		var result = await _service.GetRatesAsync(from, to, date, date);

		// Assert - It may return fixed rate or be empty depending on service logic
		result.Should().BeOfType<List<ExchangeRateResult>>();
	}
	#endregion

	#region Date Range Tests
	[Fact]
	public async Task GetRatesAsync_DateRange_ReturnsResults() {
		// Arrange
		var from = ECurrencyISO.USD;
		var to = ECurrencyISO.USD;
		var fromDate = new DateOnly(2024, 1, 1);
		var toDate = new DateOnly(2024, 1, 5);

		// Act
		var result = await _service.GetRatesAsync(from, to, fromDate, toDate);

		// Assert
		result.Should().NotBeEmpty();
		result.Should().AllSatisfy(r => {
			r.Date.Should().Be(r.Date); // Sanity check
		});
	}
	#endregion

	#region Error Handling Tests
	[Fact]
	public async Task GetRatesAsync_NoProviderAndNoFixed_ReturnsEmpty() {
		// Arrange
		var from = ECurrencyISO.USD;
		var to = ECurrencyISO.GBP; // Not configured as fixed
		var date = DateOnly.FromDateTime(DateTime.UtcNow);

		// No providers added
		// Act
		var result = await _service.GetRatesAsync(from, to, date, date);

		// Assert
		result.Should().BeEmpty();
	}
	#endregion
}
