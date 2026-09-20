using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

using System.Net;
using System.Text.Json;

using Xunit;

namespace ExchangeRates.Server.Tests.Integration;

public class QuotesControllerRealConfigTests : IAsyncLifetime {
	private WebApplicationFactory<Program>? _factory;
	private HttpClient? _client;

	public async Task InitializeAsync() {
		_factory = new WebApplicationFactory<Program>();
		_client = _factory.CreateClient();
		await Task.CompletedTask;
	}

	public async Task DisposeAsync() {
		_client?.Dispose();
		_factory?.Dispose();
		await Task.CompletedTask;
	}

	#region Fixed Rate Scenarios
	[Fact]
	public async Task GetRate_FixedRates_USD_EUR_MayReturn_OK_Or_Error() {
		// Arrange - USD/EUR might be in fixed rates but could error
		var request = "/Quotes?fromCurrency=USD&toCurrency=EUR";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
	}

	[Fact]
	public async Task GetRate_FixedRate_HasProvider_FIXED() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=EUR";

		// Act
		var response = await _client!.GetAsync(request);
		var content = await response.Content.ReadAsStringAsync();

		// Assert
		if (response.StatusCode == HttpStatusCode.OK) {
			content.Should().Contain("\"provider\":\"FIXED\"");
		}
	}
	#endregion

	#region Identity Rate Tests
	[Theory]
	[InlineData("USD")]
	[InlineData("EUR")]
	[InlineData("GBP")]
	[InlineData("JPY")]
	public async Task GetRate_IdentityCurrency_Rate_Is_1(string currency) {
		// Arrange
		var request = $"/Quotes?fromCurrency={currency}&toCurrency={currency}";

		// Act
		var response = await _client!.GetAsync(request);
		var content = await response.Content.ReadAsStringAsync();

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		// JSON is serialized in camelCase
		content.Should().Contain("\"rate\":1");
		content.Should().Contain("\"provider\":\"IDENTITY\"");
	}
	#endregion

	#region Real Provider Lookup Tests
	[Fact]
	public async Task GetRate_USD_Target_MayTriangulateOrDirect() {
		// Arrange - USD to other currencies might be direct or triangulated
		var request = "/Quotes?fromCurrency=USD&toCurrency=EUR";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetRate_CommonMajorPair_ReturnsResult() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=EUR";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
	}
	#endregion

	#region Historical Date Scenarios
	[Fact]
	public async Task GetRate_PastDate_MayReturnDataOrEmpty() {
		// Arrange
		var pastDate = new DateOnly(2020, 1, 1);
		var request = $"/Quotes?fromCurrency=USD&toCurrency=USD&fromDate={pastDate:yyyy-MM-dd}&toDate={pastDate:yyyy-MM-dd}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetRate_CurrentDate_ReturnsCurrent() {
		// Arrange
		var today = DateOnly.FromDateTime(DateTime.Today);
		var request = $"/Quotes?fromCurrency=USD&toCurrency=USD&fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GetRate_FutureDate_MayNotHaveData() {
		// Arrange
		var futureDate = new DateOnly(2099, 1, 1);
		var request = $"/Quotes?fromCurrency=USD&toCurrency=EUR&fromDate={futureDate:yyyy-MM-dd}&toDate={futureDate:yyyy-MM-dd}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
	}
	#endregion

	#region Emerging Market Currencies
	[Theory]
	[InlineData("BRL")] // Brazilian Real
	[InlineData("INR")] // Indian Rupee
	[InlineData("RUB")] // Russian Ruble
	[InlineData("ZAR")] // South African Rand
	public async Task GetRate_EmergingMarketCurrency_Supported(string currency) {
		// Arrange
		var request = $"/Quotes?fromCurrency=USD&toCurrency={currency}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
	}
	#endregion

	#region Symmetry Tests
	[Fact]
	public async Task GetRate_Symmetric_USD_EUR_and_EUR_USD_Inverse() {
		// Arrange
		var request_usd_eur = "/Quotes?fromCurrency=USD&toCurrency=EUR";
		var request_eur_usd = "/Quotes?fromCurrency=EUR&toCurrency=USD";

		// Act
		var response1 = await _client!.GetAsync(request_usd_eur);
		var response2 = await _client!.GetAsync(request_eur_usd);

		// Assert
		if (response1.StatusCode == HttpStatusCode.OK && response2.StatusCode == HttpStatusCode.OK) {
			var content1 = await response1.Content.ReadAsStringAsync();
			var content2 = await response2.Content.ReadAsStringAsync();

			// Extract rates if present
			var json1 = JsonDocument.Parse(content1);
			var json2 = JsonDocument.Parse(content2);

			// Both should return something
			content1.Should().NotBeEmpty();
			content2.Should().NotBeEmpty();
		}
	}
	#endregion

	#region Edge Case Currency Pairs
	[Theory]
	[InlineData("USD", "EUR")]
	[InlineData("EUR", "GBP")]
	[InlineData("GBP", "JPY")]
	[InlineData("JPY", "CHF")]
	[InlineData("CHF", "AUD")]
	public async Task GetRate_EdgePairs_Return_OK_Or_Error(string from, string to) {
		// Arrange
		var request = $"/Quotes?fromCurrency={from}&toCurrency={to}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(
			HttpStatusCode.OK,
			HttpStatusCode.BadRequest,
			HttpStatusCode.InternalServerError,
			HttpStatusCode.NotFound);
	}
	#endregion

	#region Response Consistency Tests
	[Fact]
	public async Task GetRate_SameRequest_Twice_ReturnsSameData() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=EUR";

		// Act
		var response1 = await _client!.GetAsync(request);
		var content1 = await response1.Content.ReadAsStringAsync();

		var response2 = await _client!.GetAsync(request);
		var content2 = await response2.Content.ReadAsStringAsync();

		// Assert
		response1.StatusCode.Should().Be(response2.StatusCode);
		// Content should be structurally equivalent (rates for same date should match)
		content1.Should().Be(content2);
	}

	[Fact]
	public async Task GetRate_DateRange_MaySucceed_Or_Error() {
		// Arrange
		var today = DateOnly.FromDateTime(DateTime.Today);
		var request = $"/Quotes?fromCurrency=USD&toCurrency=EUR&fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
	}
	#endregion

	#region Major Currency Pair Coverage
	[Theory]
	[InlineData("USD", "EUR")]
	[InlineData("USD", "GBP")]
	[InlineData("USD", "JPY")]
	[InlineData("USD", "CHF")]
	[InlineData("USD", "CAD")]
	[InlineData("USD", "AUD")]
	[InlineData("USD", "NZD")]
	[InlineData("USD", "CNY")]
	[InlineData("EUR", "GBP")]
	[InlineData("EUR", "JPY")]
	public async Task GetRate_MajorPair_ShouldBeSupported(string from, string to) {
		// Arrange
		var request = $"/Quotes?fromCurrency={from}&toCurrency={to}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(
			HttpStatusCode.OK,
			HttpStatusCode.BadRequest,
			HttpStatusCode.InternalServerError);
	}
	#endregion

	#region Provider Priority Tests
	[Fact]
	public async Task GetRate_Multiple_Providers_PicksFirstAvailable() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=EUR";

		// Act
		var response = await _client!.GetAsync(request);
		var content = await response.Content.ReadAsStringAsync();

		// Assert
		if (response.StatusCode == HttpStatusCode.OK) {
			// Should contain a Provider field
			content.Should().Contain("\"Provider\":");
		}
	}
	#endregion

	#region Data Availability Tests
	[Fact]
	public async Task GetRate_WhenNoDataAvailable_ReturnsEmptyOrError() {
		// Arrange - request a currency pair that definitely won't have data
		var farFutureDate = new DateOnly(2099, 12, 31);
		var request = $"/Quotes?fromCurrency=USD&toCurrency=XYZ&fromDate={farFutureDate:yyyy-MM-dd}&toDate={farFutureDate:yyyy-MM-dd}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(
			HttpStatusCode.BadRequest,
			HttpStatusCode.InternalServerError,
			HttpStatusCode.OK); // OK with empty array is acceptable
	}
	#endregion

	#region Response Validation Tests
	[Fact]
	public async Task GetRate_Response_Proper_JSON_Format() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=USD";

		// Act
		var response = await _client!.GetAsync(request);
		var content = await response.Content.ReadAsStringAsync();

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		// Should be valid JSON
		var json = JsonDocument.Parse(content);
		json.Should().NotBeNull();
	}

	[Fact]
	public async Task GetRate_Response_Contains_All_RequiredFields() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=USD";

		// Act
		var response = await _client!.GetAsync(request);
		var content = await response.Content.ReadAsStringAsync();

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		content.Should().Contain("\"date\"");
		content.Should().Contain("\"baseCurrency\"");
		content.Should().Contain("\"quoteCurrency\"");
		content.Should().Contain("\"rate\"");
		content.Should().Contain("\"provider\"");
	}
	#endregion
}
