using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;

namespace ExchangeRates.Server.Tests.Fixtures;

/// <summary>
/// Test provider that implements ICentralBankProvider for testing
/// </summary>
public sealed class TestCentralBankProvider : ICentralBankProvider {
	private readonly Dictionary<(ECurrencyISO, ECurrencyISO, DateOnly), decimal> _rates;
	private readonly HashSet<ECurrencyISO> _supported;

	public string Code { get; }
	public string Name { get; }
	public ECurrencyISO NativeCurrency { get; }
	public IReadOnlySet<ECurrencyISO> SupportedCurrencies { get; }

	public TestCentralBankProvider(
		string code,
		string name,
		ECurrencyISO nativeCurrency,
		IEnumerable<ECurrencyISO> supportedCurrencies,
		Dictionary<(ECurrencyISO, ECurrencyISO, DateOnly), decimal>? rates = null) {
		Code = code;
		Name = name;
		NativeCurrency = nativeCurrency;
		_supported = new HashSet<ECurrencyISO>(supportedCurrencies);
		SupportedCurrencies = _supported;
		_rates = rates ?? [];
	}

	public bool Supports(ECurrencyISO currency) => _supported.Contains(currency);

	public Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(
		ECurrencyISO currency,
		DateOnly fromDate,
		DateOnly toDate,
		CancellationToken cancellationToken = default) {

		var result = new List<ExchangeRate>();
		var currentDate = fromDate;

		while (currentDate <= toDate) {
			// Try to find rates for all quote currencies on this date
			foreach (var quoteCurrency in _supported.Where(c => c != currency)) {
				var key = (currency, quoteCurrency, currentDate);
				if (_rates.TryGetValue(key, out var rate)) {
					result.Add(new ExchangeRate(currentDate, currency, quoteCurrency, rate, Code));
				}
			}
			currentDate = currentDate.AddDays(1);
		}

		return Task.FromResult<IReadOnlyList<ExchangeRate>>(result);
	}
}

/// <summary>
/// Builder for test data
/// </summary>
public static class MockDataBuilder {
	public static ExchangeRate CreateRate(
		ECurrencyISO from = ECurrencyISO.USD,
		ECurrencyISO to = ECurrencyISO.EUR,
		decimal rate = 0.92m,
		string? providerCode = null,
		DateOnly? date = null) {
		return new ExchangeRate(
			date ?? DateOnly.FromDateTime(DateTime.UtcNow),
			from,
			to,
			rate,
			providerCode ?? "TEST");
	}

	public static List<ExchangeRate> CreateRateRange(
		ECurrencyISO from,
		ECurrencyISO to,
		decimal startRate,
		int dayCount,
		DateOnly? startDate = null,
		string? providerCode = null) {
		var rates = new List<ExchangeRate>();
		var date = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
		var step = 0.01m / dayCount;

		for (int i = 0; i < dayCount; i++) {
			rates.Add(new ExchangeRate(
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
