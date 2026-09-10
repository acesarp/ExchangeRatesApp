using ExchangeRates.Domain.Entities;
using ExchangeRates.Server.Interfaces;

namespace ExchangeRates.Server.Tests.Fixtures;

/// <summary>
/// Test provider that implements ICentralBankProvider for testing
/// </summary>
public sealed class TestCentralBankProvider : ICentralBankProvider {
	private readonly Dictionary<(string, string, DateOnly), decimal> _rates;
	private readonly HashSet<string> _supported;
	public IReadOnlySet<string> SupportedCurrencies { get; }

	public string BankName { get; set; } = "Bank name";

	public string NativeCurrencyCode { get; set; } = "EUR";
	public int CurrencyId => 1;

	public string CountryOfOrigin { get; set; } = "Test country";

	public List<string> HistoricCurrencies => new();

	public string PivotCurrency => "USD";

	public string BankCode { get; set; } = "EUB";

	public int? Priority { get; set; } = 1;

	protected CentralBankEntity Bank => new CentralBankEntity(BankCode, BankName, CountryOfOrigin, CurrencyId, true, DateTime.UtcNow);

	public TestCentralBankProvider(
		string code,
		string name,
		string nativeCurrency,
		IEnumerable<string> supportedCurrencies,
		Dictionary<(string, string, DateOnly), decimal>? rates = null) {
		BankCode = code;
		BankName = name;
		NativeCurrencyCode = nativeCurrency;
		_supported = new HashSet<string>(supportedCurrencies);
		SupportedCurrencies = _supported;
		_rates = rates ?? new Dictionary<(string, string, DateOnly), decimal>();
	}

	public bool Supports(string currency) => _supported.Contains(currency);

	public Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(string currency, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) {

		var result = new List<ExchangeRateResult>();
		var currentDate = fromDate;

		while (currentDate <= toDate) {
			var key = (NativeCurrencyCode, currency, currentDate);
			if (_rates.TryGetValue(key, out var rate)) {
				result.Add(new ExchangeRateResult(currentDate, NativeCurrencyCode, currency, rate, BankCode));
			}
			currentDate = currentDate.AddDays(1);
		}

		return Task.FromResult<IReadOnlyList<ExchangeRateResult>>(result);
	}
}

/// <summary>
/// Builder for test data
/// </summary>
public static class MockDataBuilder {
	public static ExchangeRateResult CreateRate(
		string from = "USD",
		string to = "EUR",
		decimal rate = 0.92m,
		string? providerCode = null,
		DateOnly? date = null) {
		return new ExchangeRateResult(
			date ?? DateOnly.FromDateTime(DateTime.UtcNow),
			from,
			to,
			rate,
			providerCode ?? "TEST");
	}

	public static List<ExchangeRateResult> CreateRateRange(
		string from,
		string to,
		decimal startRate,
		int dayCount,
		DateOnly? startDate = null,
		string? providerCode = null) {
		var rates = new List<ExchangeRateResult>();
		var date = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
		var step = 0.01m / dayCount;

		for (int i = 0; i < dayCount; i++) {
			rates.Add(new ExchangeRateResult(
				date,
				from,
				to,
				startRate + (step * i),
				providerCode ?? "TEST"));
			date = date.AddDays(1);
		}

		return rates;
	}

	public static TestCentralBankProvider CreateTestProvider(
		string code = "TEST",
		string nativeCurrency = "USD",
		string[]? supportedCurrencies = null,
		Dictionary<(string, string, DateOnly), decimal>? rates = null) {
		var currencies = supportedCurrencies ?? new[] { "USD", "EUR", "JPY" };
		return new TestCentralBankProvider(
			code,
			$"Test Central Bank {code}",
			nativeCurrency,
			currencies,
			rates);
	}

	public static Dictionary<(string, string, DateOnly), decimal> CreateRateDictionary(
		string from,
		string to,
		decimal rate,
		DateOnly? date = null) {
		var d = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
		return new() { { (from, to, d), rate } };
	}
}
