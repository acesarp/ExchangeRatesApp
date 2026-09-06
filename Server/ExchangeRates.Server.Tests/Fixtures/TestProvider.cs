using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;

namespace ExchangeRates.Server.Tests.Fixtures;

/// <summary>
/// Test provider that implements ICentralBankProvider for testing
/// </summary>
public sealed class TestCentralBankProvider : ICentralBankProvider {
	private readonly Dictionary<(ECurrencyISO, ECurrencyISO, DateOnly), decimal> _rates;
	private readonly HashSet<ECurrencyISO> _supported;

	public string Code { get; }
	public IReadOnlySet<ECurrencyISO> SupportedCurrencies { get; }
	public bool InverseProvider { get; set; }

	public string BankName => "Bank name";

	public ECurrencyISO NativeCurrency => ECurrencyISO.EUR;

	public string CountryOfOrigin => "Test country";

	public List<string> HistoricCurrencies => new();

	public ECurrencyISO PivotCurrency => ECurrencyISO.USD;

	public TestCentralBankProvider(
		string code,
		string name,
		ECurrencyISO nativeCurrency,
		IEnumerable<ECurrencyISO> supportedCurrencies,
		Dictionary<(ECurrencyISO, ECurrencyISO, DateOnly), decimal>? rates = null) {
		Code = code;
		_supported = new HashSet<ECurrencyISO>(supportedCurrencies);
		SupportedCurrencies = _supported;
		_rates = rates ?? new Dictionary<(ECurrencyISO, ECurrencyISO, DateOnly), decimal>();
	}

	public bool Supports(ECurrencyISO currency) => _supported.Contains(currency);

	public Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(ECurrencyISO currency, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) {

		var result = new List<ExchangeRateResult>();
		var currentDate = fromDate;

		while (currentDate <= toDate) {
			var key = (NativeCurrency, currency, currentDate);
			if (_rates.TryGetValue(key, out var rate)) {
				result.Add(new ExchangeRateResult(currentDate, NativeCurrency, currency, rate, Code));
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
		ECurrencyISO from = ECurrencyISO.USD,
		ECurrencyISO to = ECurrencyISO.EUR,
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
		ECurrencyISO from,
		ECurrencyISO to,
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
		ECurrencyISO nativeCurrency = ECurrencyISO.USD,
		ECurrencyISO[]? supportedCurrencies = null,
		Dictionary<(ECurrencyISO, ECurrencyISO, DateOnly), decimal>? rates = null) {
		var currencies = supportedCurrencies ?? new[] { ECurrencyISO.USD, ECurrencyISO.EUR, ECurrencyISO.JPY };
		return new TestCentralBankProvider(
			code,
			$"Test Central Bank {code}",
			nativeCurrency,
			currencies,
			rates);
	}

	public static Dictionary<(ECurrencyISO, ECurrencyISO, DateOnly), decimal> CreateRateDictionary(
		ECurrencyISO from,
		ECurrencyISO to,
		decimal rate,
		DateOnly? date = null) {
		var d = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
		return new() { { (from, to, d), rate } };
	}
}
