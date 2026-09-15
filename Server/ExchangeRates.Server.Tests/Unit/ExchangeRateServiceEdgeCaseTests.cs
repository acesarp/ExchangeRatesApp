using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;
using ExchangeRates.Server.Services;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace ExchangeRates.Server.Tests.Unit;

public class ExchangeRateServiceEdgeCaseTests {
	private readonly IExchangeRateService _service;
	private readonly IConfiguration _config;
	private readonly IServiceProvider _serviceProvider;

	public ExchangeRateServiceEdgeCaseTests() {
		var configDict = new Dictionary<string, string?> {
			["PivotCurrency"] = "USD"
		};

		_config = new ConfigurationBuilder()
			.AddInMemoryCollection(configDict)
			.Build();

		var services = new ServiceCollection();

		services.AddSingleton<IConfiguration>(_config);
		services.AddLogging(builder => builder.AddConsole());
		services.AddHttpClient();

		// IExchangeRateRepository precisa ser registrado/mockado aqui.
		// services.AddSingleton<IExchangeRateRepository>(repository);

		_serviceProvider = services.BuildServiceProvider();

		var repository = _serviceProvider.GetRequiredService<IExchangeRateRepository>();
		var factoryLogger = _serviceProvider.GetRequiredService<ILogger<CentralBankProviderFactory>>();
		var serviceLogger = _serviceProvider.GetRequiredService<ILogger<ExchangeRateService>>();

		var factory = new CentralBankProviderFactory(_serviceProvider, factoryLogger);

		_service = new ExchangeRateService(serviceLogger, repository, factory, _config);
	}

	[Fact]
	public async Task GetRatesAsync_LargeDateRange_SameCurrency_ReturnsIdentityRates() {
		var from = "USD";
		var to = "USD";
		var fromDate = new DateOnly(2023, 1, 1);
		var toDate = new DateOnly(2023, 12, 31);

		var sw = System.Diagnostics.Stopwatch.StartNew();

		var result = await _service.GetRatesAsync(from, to, fromDate, toDate);

		sw.Stop();

		result.Should().NotBeEmpty();
		sw.ElapsedMilliseconds.Should().BeLessThan(5000);
	}

	[Fact]
	public async Task GetRatesAsync_SameCurrency_ReturnsRateOne() {
		var date = new DateOnly(2024, 1, 1);

		var result = await _service.GetRatesAsync("USD", "USD", date, date);

		result.Should().NotBeEmpty();
		result[0].Rate.Should().Be(1m);
		result[0].BaseCurrency.Should().Be("USD");
		result[0].QuoteCurrency.Should().Be("USD");
	}

	[Fact]
	public async Task GetRatesAsync_InvalidDateRange_ThrowsArgumentException() {
		var fromDate = new DateOnly(2024, 1, 10);
		var toDate = new DateOnly(2024, 1, 1);

		var act = async () => await _service.GetRatesAsync("USD", "EUR", fromDate, toDate);

		await act.Should().ThrowAsync<ArgumentException>();
	}
}