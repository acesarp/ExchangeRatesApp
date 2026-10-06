using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ExchangeRates.Server.Tests.Fixtures;

using FluentAssertions;

using Moq;

using Xunit;

namespace ExchangeRates.Server.Tests.Integration;

public class QuotesControllerTests : IAsyncLifetime {
	private ApiFactory _factory = null!;
	private HttpClient _client = null!;

	public Task InitializeAsync() {
		_factory = new ApiFactory();
		_client = _factory.CreateClient();
		return Task.CompletedTask;
	}

	public async Task DisposeAsync() {
		_client.Dispose();
		await _factory.DisposeAsync();
	}

	private sealed record RateDto(DateOnly Date, string BaseCurrency, string QuoteCurrency, decimal Rate, string Provider);

	private sealed record CurrencyDto(string CurrencyCode, string Name);

	#region exchange-rates
	[Fact]
	public async Task ExchangeRates_SameCurrency_ReturnsIdentityRateAsJson() {
		var response = await _client.GetAsync("/api/exchange-rates?baseCurrency=USD&quoteCurrency=USD&fromDate=2026-10-01&toDate=2026-10-01");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

		var rates = await response.Content.ReadFromJsonAsync<List<RateDto>>();
		rates.Should().ContainSingle().Which.Should().Be(new RateDto(new DateOnly(2026, 10, 1), "USD", "USD", 1m, "IDENTITY"));
	}

	[Fact]
	public async Task ExchangeRates_UsesCamelCaseFieldNames() {
		var content = await _client.GetStringAsync("/api/exchange-rates?baseCurrency=USD&quoteCurrency=USD&fromDate=2026-10-01&toDate=2026-10-01");

		var item = JsonDocument.Parse(content).RootElement[0];
		item.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("date", "baseCurrency", "quoteCurrency", "rate", "provider");
	}

	[Fact]
	public async Task ExchangeRates_LowercaseCurrencies_AreNormalized() {
		var rates = await _client.GetFromJsonAsync<List<RateDto>>("/api/exchange-rates?baseCurrency=usd&quoteCurrency=usd&fromDate=2026-10-01&toDate=2026-10-01");

		rates.Should().ContainSingle();
		rates![0].BaseCurrency.Should().Be("USD");
		rates[0].QuoteCurrency.Should().Be("USD");
	}

	[Fact]
	public async Task ExchangeRates_DateRange_ReturnsOneRatePerDay() {
		var rates = await _client.GetFromJsonAsync<List<RateDto>>("/api/exchange-rates?baseCurrency=EUR&quoteCurrency=EUR&fromDate=2026-10-01&toDate=2026-10-07");

		rates.Should().HaveCount(7);
		rates!.Select(r => r.Date).Should().BeInAscendingOrder();
	}

	[Fact]
	public async Task ExchangeRates_CachedRate_IsReturnedFromRepository() {
		var day = new DateOnly(2026, 10, 1);
		_factory.CacheUsdEurRate(day, 0.92m);

		var rates = await _client.GetFromJsonAsync<List<RateDto>>("/api/exchange-rates?baseCurrency=USD&quoteCurrency=EUR&fromDate=2026-10-01&toDate=2026-10-01");

		rates.Should().ContainSingle().Which.Should().Be(new RateDto(day, "USD", "EUR", 0.92m, "TEST"));
	}

	[Fact]
	public async Task ExchangeRates_CachedRate_IsInvertedForOppositeDirection() {
		_factory.CacheUsdEurRate(new DateOnly(2026, 10, 1), 0.8m);

		var rates = await _client.GetFromJsonAsync<List<RateDto>>("/api/exchange-rates?baseCurrency=EUR&quoteCurrency=USD&fromDate=2026-10-01&toDate=2026-10-01");

		rates.Should().ContainSingle();
		rates![0].BaseCurrency.Should().Be("EUR");
		rates[0].QuoteCurrency.Should().Be("USD");
		rates[0].Rate.Should().Be(1.25m);
	}

	[Fact]
	public async Task ExchangeRates_AllDatesUnavailable_ReturnsEmptyList() {
		var day = new DateOnly(2026, 10, 3);
		_factory.Repository.Setup(x => x.GetUnavailableDatesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync([day]);

		var response = await _client.GetAsync("/api/exchange-rates?baseCurrency=USD&quoteCurrency=EUR&fromDate=2026-10-03&toDate=2026-10-03");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		(await response.Content.ReadFromJsonAsync<List<RateDto>>()).Should().BeEmpty();
	}

	[Fact]
	public async Task ExchangeRates_FromDateAfterToDate_ReturnsBadRequest() {
		var response = await _client.GetAsync("/api/exchange-rates?baseCurrency=USD&quoteCurrency=EUR&fromDate=2026-10-10&toDate=2026-10-01");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		(await response.Content.ReadAsStringAsync()).Should().Contain("fromDate cannot be greater than toDate");
	}

	[Fact]
	public async Task ExchangeRates_InvalidDate_ReturnsBadRequest() {
		var response = await _client.GetAsync("/api/exchange-rates?baseCurrency=USD&quoteCurrency=EUR&fromDate=not-a-date");

		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}
	#endregion

	#region available-currencies
	[Fact]
	public async Task AvailableCurrencies_ReturnsCurrenciesFromRepository() {
		var currencies = await _client.GetFromJsonAsync<List<CurrencyDto>>("/api/available-currencies");

		currencies!.Select(c => c.CurrencyCode).Should().Equal("USD", "EUR");
		currencies![0].Name.Should().Be("US Dollar");
	}
	#endregion

	#region environment
	[Fact]
	public async Task Environment_ReturnsHostingEnvironmentName() {
		var content = await _client.GetStringAsync("/api/environment");

		JsonDocument.Parse(content).RootElement.GetProperty("environment").GetString().Should().Be("Development");
	}
	#endregion
}
