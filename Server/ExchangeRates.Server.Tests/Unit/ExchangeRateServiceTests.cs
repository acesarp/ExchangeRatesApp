using ExchangeRates.Domain.Entities;
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

public class ExchangeRateServiceTests {
	private static readonly CurrencyEntity Usd = new("USD", 840, "US Dollar", false, 1) { Id = 1 };
	private static readonly CurrencyEntity Eur = new("EUR", 978, "Euro", false, 2) { Id = 2 };

	private readonly IExchangeRateService _service;
	private readonly Mock<IExchangeRateRepository> _repository;

	public ExchangeRateServiceTests() {
		_repository = new Mock<IExchangeRateRepository>();
		_repository.Setup(x => x.GetRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		_repository.Setup(x => x.GetUnavailableDatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		_repository.Setup(x => x.GetFixedRatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);
		_repository.Setup(x => x.GetBanksPublishingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["PivotCurrency"] = "USD" })
			.Build();

		var services = new ServiceCollection()
			.AddSingleton<IConfiguration>(config)
			.AddLogging()
			.BuildServiceProvider();

		var factory = new CentralBankProviderFactory(services, NullLogger<CentralBankProviderFactory>.Instance);

		_service = new ExchangeRateService(NullLogger<ExchangeRateService>.Instance, _repository.Object, factory, config);
	}

	#region Identity Tests
	[Fact]
	public async Task GetRatesAsync_SameCurrency_ReturnsIdentity() {
		// Arrange
		var from = "USD";
		var to = "USD";
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

	#region Repository Cache Tests
	[Fact]
	public async Task GetRatesAsync_RatesCachedInRepository_ReturnsThemWithoutCallingProvider() {
		// Arrange
		var date = new DateOnly(2026, 10, 1);
		CacheRate(Usd, Eur, 0.92m, date);

		// Act
		var result = await _service.GetRatesAsync("USD", "EUR", date, date);

		// Assert
		result.Should().ContainSingle();
		result[0].BaseCurrency.Should().Be("USD");
		result[0].QuoteCurrency.Should().Be("EUR");
		result[0].Rate.Should().Be(0.92m);
		_repository.Verify(x => x.GetBanksPublishingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task GetRatesAsync_RateCachedInOppositeDirection_ReturnsInvertedRate() {
		// Arrange - the repository stores the canonical USD/EUR pair
		var date = new DateOnly(2026, 10, 1);
		CacheRate(Usd, Eur, 0.8m, date);

		// Act
		var result = await _service.GetRatesAsync("EUR", "USD", date, date);

		// Assert
		result.Should().ContainSingle();
		result[0].BaseCurrency.Should().Be("EUR");
		result[0].QuoteCurrency.Should().Be("USD");
		result[0].Rate.Should().Be(1.25m);
	}
	#endregion

	#region Date Range Tests
	[Fact]
	public async Task GetRatesAsync_DateRange_ReturnsResults() {
		// Arrange
		var from = "USD";
		var to = "USD";
		var fromDate = new DateOnly(2024, 1, 1);
		var toDate = new DateOnly(2024, 1, 5);

		// Act
		var result = await _service.GetRatesAsync(from, to, fromDate, toDate);

		// Assert
		result.Should().HaveCount(5);
		result.Select(r => r.Date).Should().BeEquivalentTo(Enumerable.Range(0, 5).Select(fromDate.AddDays));
	}
	#endregion

	#region Unavailable Date Tests
	[Fact]
	public async Task GetRatesAsync_AllDatesMarkedUnavailable_ReturnsEmptyWithoutCallingProvider() {
		// Arrange
		var date = new DateOnly(2026, 10, 3);
		_repository.Setup(x => x.GetUnavailableDatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([date]);

		// Act
		var result = await _service.GetRatesAsync("USD", "GBP", date, date);

		// Assert
		result.Should().BeEmpty();
		_repository.Verify(x => x.GetBanksPublishingAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
	}
	#endregion

	private void CacheRate(CurrencyEntity baseCurrency, CurrencyEntity quoteCurrency, decimal rate, DateOnly date) {
		var entity = new ExchangeRateEntity {
			Date = date,
			BaseCurrency = baseCurrency,
			BaseCurrencyId = baseCurrency.Id,
			QuoteCurrency = quoteCurrency,
			QuoteCurrencyId = quoteCurrency.Id,
			Rate = rate,
			Provider = "TEST"
		};

		_repository.Setup(x => x.GetRatesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([entity]);
	}
}
