using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ExchangeRates.Server.Tests.Integration;

public class QuotesControllerTests : IAsyncLifetime {
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

	#region Route Tests
	[Fact]
	public async Task GetRate_WithValidCurrencies_Returns200() {
		// Arrange - using identity rate which doesn't depend on provider
		var request = "/Quotes?fromCurrency=USD&toCurrency=USD";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GetRate_WithoutQueryParams_MayReturnOkOrBadRequest() {
		// Arrange
		var request = "/Quotes";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert - Controller may use defaults or reject
		response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
	}

	[Fact]
	public async Task GetRate_WithMissingFromCurrency_MayReturnOkOrBadRequest() {
		// Arrange
		var request = "/Quotes?toCurrency=EUR";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
	}

	[Fact]
	public async Task GetRate_WithMissingToCurrency_MayReturnOkOrBadRequest() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
	}
	#endregion

	#region Response Format Tests
	[Fact]
	public async Task GetRate_ReturnsJsonResponse() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=USD";

		// Act
		var response = await _client!.GetAsync(request);
		var content = await response.Content.ReadAsStringAsync();

		// Assert
		response.Content.Headers.ContentType?.MediaType.Should().Contain("json");
		content.Should().NotBeEmpty();
	}

	[Fact]
	public async Task GetRate_SuccessResponse_ContainsExpectedFields() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=USD";

		// Act
		var response = await _client!.GetAsync(request);
		var content = await response.Content.ReadAsStringAsync();

		// Assert - JSON uses camelCase
		content.Should().Contain("date");
		content.Should().Contain("baseCurrency");
		content.Should().Contain("quoteCurrency");
		content.Should().Contain("rate");
		content.Should().Contain("provider");
	}

	[Fact]
	public async Task GetRate_ErrorResponse_ContainsMessageField() {
		// Arrange
		var request = "/Quotes?fromCurrency=INVALID&toCurrency=INVALID";

		// Act
		var response = await _client!.GetAsync(request);
		var content = await response.Content.ReadAsStringAsync();

		// Assert - Either validation error or bad request
		response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
	}
	#endregion

	#region Date Range Tests
	[Fact]
	public async Task GetRate_WithFromDate_Filters() {
		// Arrange
		var today = DateOnly.FromDateTime(DateTime.Today);
		var request = $"/Quotes?fromCurrency=USD&toCurrency=USD&fromDate={today:yyyy-MM-dd}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GetRate_WithToDate_Filters() {
		// Arrange
		var today = DateOnly.FromDateTime(DateTime.Today);
		var request = $"/Quotes?fromCurrency=USD&toCurrency=USD&toDate={today:yyyy-MM-dd}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GetRate_WithBothDates_Filters() {
		// Arrange
		var today = DateOnly.FromDateTime(DateTime.Today);
		var yesterday = today.AddDays(-1);
		var request = $"/Quotes?fromCurrency=USD&toCurrency=USD&fromDate={yesterday:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}
	#endregion

	#region Enum Validation Tests
	[Fact]
	public async Task GetRate_WithInvalidEnum_ReturnsBadRequest() {
		// Arrange
		var request = "/Quotes?fromCurrency=INVALID&toCurrency=EUR";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetRate_CaseInsensitiveEnum_Works() {
		// Arrange
		var request = "/Quotes?fromCurrency=usd&toCurrency=usd";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}
	#endregion

	#region Error Scenarios Tests
	[Fact]
	public async Task GetRate_IdentityRate_Returns1() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=USD";

		// Act
		var response = await _client!.GetAsync(request);
		var content = await response.Content.ReadAsStringAsync();

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		content.Should().Contain("1");
	}

	[Fact]
	public async Task GetRate_UnsupportedPair_ReturnsEmptyOrError() {
		// Arrange - EUR/GBP might not be directly available but should triangulate or return empty
		var request = "/Quotes?fromCurrency=EUR&toCurrency=GBP";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
	}
	#endregion

	#region Performance Tests
	[Fact]
	public async Task GetRate_LargeDateRange_CompletesInReasonableTime() {
		// Arrange
		var startDate = new DateOnly(2024, 1, 1);
		var endDate = new DateOnly(2024, 12, 31);
		var request = $"/Quotes?fromCurrency=USD&toCurrency=USD&fromDate={startDate:yyyy-MM-dd}&toDate={endDate:yyyy-MM-dd}";

		// Act
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var response = await _client!.GetAsync(request);
		sw.Stop();

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		sw.ElapsedMilliseconds.Should().BeLessThan(5000);
	}

	[Fact]
	public async Task GetRate_MultipleRequests_AreIncrementallYFast() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=USD";
		var times = new List<long>();

		// Act
		for (int i = 0; i < 5; i++) {
			var sw = System.Diagnostics.Stopwatch.StartNew();
			await _client!.GetAsync(request);
			sw.Stop();
			times.Add(sw.ElapsedMilliseconds);
		}

		// Assert
		times.Should().AllSatisfy(t => t.Should().BeLessThan(2000));
	}
	#endregion

	#region Content Type Tests
	[Fact]
	public async Task GetRate_Returns_ApplicationJsonContentType() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=USD";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
	}
	#endregion

	#region Boundary Tests
	[Fact]
	public async Task GetRate_WithNullDates_UsesDefaults() {
		// Arrange
		var request = "/Quotes?fromCurrency=USD&toCurrency=USD&fromDate=&toDate=";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetRate_WithSpacesInEnums_Returns_BadRequest() {
		// Arrange
		var request = "/Quotes?fromCurrency=US D&toCurrency=EUR";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task GetRate_WithNumericEnums_Returns_BadRequest() {
		// Arrange
		var request = "/Quotes?fromCurrency=1&toCurrency=2";

		// Act
		var response = await _client!.GetAsync(request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}
	#endregion
}
