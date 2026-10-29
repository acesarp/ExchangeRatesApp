using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Infrastructure.Providers;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Services;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Xunit;

namespace ExchangeRates.Server.Tests.Unit;

public class ExchangeRateServiceEdgeCaseTests {
	private readonly IExchangeRateService _service;

	public ExchangeRateServiceEdgeCaseTests() {
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["PivotCurrency"] = "USD" })
			.Build();

		// None of these cases should reach the repository: identity pairs and invalid ranges are resolved up front.
		var repository = new Mock<IExchangeRateRepository>(MockBehavior.Strict);

		var services = new ServiceCollection()
			.AddSingleton<IConfiguration>(config)
			.AddLogging()
			.BuildServiceProvider();

		var factory = new CentralBankProviderFactory(services, NullLogger<CentralBankProviderFactory>.Instance);

		_service = new ExchangeRateService(NullLogger<ExchangeRateService>.Instance, repository.Object, factory, config);
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

		result.Should().HaveCount(365);
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
