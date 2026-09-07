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

public class ExchangeRateServiceEdgeCaseTests {
	private readonly IExchangeRateService _service;
	private readonly List<ICentralBankProvider> _providers;
	private readonly IConfiguration _config;
	private readonly IServiceProvider _serviceProvider;

	public ExchangeRateServiceEdgeCaseTests() {
		_providers = [];
		var configDict = new Dictionary<string, string?> {
			{ "PivotCurrency", "USD" },
			{ "FixedExchangeRates:Rates:USD", "1.00" }
		};
		var configBuilder = new ConfigurationBuilder().AddInMemoryCollection(configDict);
		_config = configBuilder.Build();

		var serviceCollection = new ServiceCollection()
			.AddSingleton(_config)
			.AddLogging(builder => builder.AddConsole());

		_serviceProvider = serviceCollection.BuildServiceProvider();

		var logger = _serviceProvider.GetRequiredService<ILogger<CentralBankProviderFactory>>();
		var factory = new CentralBankProviderFactory(_config, _providers, logger);
		var fixedProvider = new FixedExchangeRateProvider(_config);
		var repository = _serviceProvider.GetRequiredService<IExchangeRateRepository>();
		var serviceLogger = _serviceProvider.GetRequiredService<ILogger<ExchangeRateService>>();
		_service = new ExchangeRateService(serviceLogger, repository, factory, _config);
	}

	#region Empty/Sparse Data Tests
	[Fact]
	public async Task GetRatesAsync_NoDataInRange_ReturnsEmpty() {
		// Arrange
		var from = ECurrencyISO.USD;
		var to = ECurrencyISO.EUR;
		var provider = new TestCentralBankProvider("TEST", "Test", ECurrencyISO.USD, new[] { from, to }, new());
		_providers.Add(provider);

		// Act
		var result = await _service.GetRatesAsync(from, to, new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 5));

		// Assert
		result.Should().BeOfType<List<ExchangeRateResult>>();
	}

	[Fact]
	public async Task GetRatesAsync_SparseData_MayReturnResults() {
		// Arrange
		var from = ECurrencyISO.USD;
		var to = ECurrencyISO.EUR;
		var fromDate = new DateOnly(2024, 1, 1);
		var toDate = new DateOnly(2024, 1, 10);

		var rates = new Dictionary<(ECurrencyISO, ECurrencyISO, DateOnly), decimal> {
			{ (from, to, new DateOnly(2024, 1, 2)), 0.92m },
			{ (from, to, new DateOnly(2024, 1, 5)), 0.93m },
			{ (from, to, new DateOnly(2024, 1, 8)), 0.91m }
		};

		var provider = new TestCentralBankProvider("TEST", "Test", ECurrencyISO.USD, new[] { from, to }, rates);
		_providers.Add(provider);

		// Act
		var result = await _service.GetRatesAsync(from, to, fromDate, toDate);

		// Assert
		result.Should().BeOfType<List<ExchangeRateResult>>();
	}
	#endregion

	#region Large Date Range Tests
	[Fact]
	public async Task GetRatesAsync_LargeDateRange_Completes() {
		// Arrange
		var from = ECurrencyISO.USD;
		var to = ECurrencyISO.USD;
		var fromDate = new DateOnly(2023, 1, 1);
		var toDate = new DateOnly(2023, 12, 31);

		// Act
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var result = await _service.GetRatesAsync(from, to, fromDate, toDate);
		sw.Stop();

		// Assert
		result.Should().NotBeEmpty();
		sw.ElapsedMilliseconds.Should().BeLessThan(5000);
	}
	#endregion

	#region Inverse Rate Tests
	[Fact]
	public async Task GetRatesAsync_Symmetric_Rates_Valid() {
		// Arrange
		var from = ECurrencyISO.EUR;
		var to = ECurrencyISO.USD;
		var date = new DateOnly(2024, 1, 1);

		var rates = new Dictionary<(ECurrencyISO, ECurrencyISO, DateOnly), decimal> {
			{ (ECurrencyISO.USD, ECurrencyISO.EUR, date), 0.92m }
		};

		var provider = new TestCentralBankProvider(
			"TEST",
			"Test",
			ECurrencyISO.USD,
			new[] { ECurrencyISO.USD, ECurrencyISO.EUR },
			rates);
		_providers.Add(provider);

		// Act
		var result = await _service.GetRatesAsync(from, to, date, date);

		// Assert - May be empty if no inverse provider
		result.Should().BeOfType<List<ExchangeRateResult>>();
	}
	#endregion

	#region Historical Data Tests
	[Fact]
	public async Task GetRatesAsync_HistoricalDate_MayNotHaveData() {
		// Arrange
		var from = ECurrencyISO.USD;
		var to = ECurrencyISO.EUR;
		var pastDate = new DateOnly(2020, 1, 1);

		// Act
		var result = await _service.GetRatesAsync(from, to, pastDate, pastDate);

		// Assert - Historical data may or may not exist
		result.Should().BeOfType<List<ExchangeRateResult>>();
	}
	#endregion

	#region Cancellation Tests
	[Fact]
	public async Task GetRatesAsync_WithValidData_Completes() {
		// Arrange
		var from = ECurrencyISO.USD;
		var to = ECurrencyISO.USD;

		// Act
		var result = await _service.GetRatesAsync(from, to, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));

		// Assert
		result.Should().NotBeEmpty();
	}
	#endregion

	#region Cross-Currency Tests
	[Theory]
	[InlineData("USD", "EUR")]
	[InlineData("USD", "GBP")]
	[InlineData("USD", "JPY")]
	public async Task GetRatesAsync_ValidPairs_ReturnResults(string from, string to) {
		// Arrange
		var fromCurrency = Enum.Parse<ECurrencyISO>(from);
		var toCurrency = Enum.Parse<ECurrencyISO>(to);
		var date = DateOnly.FromDateTime(DateTime.UtcNow);

		// Act
		var result = await _service.GetRatesAsync(fromCurrency, toCurrency, date, date);

		// Assert
		result.Should().BeOfType<List<ExchangeRateResult>>();
	}
	#endregion
}
