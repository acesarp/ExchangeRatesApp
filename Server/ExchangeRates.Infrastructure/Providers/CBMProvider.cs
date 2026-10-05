
using System.Text.Json;

using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Central Bank of Myanmar
/// </summary>
public sealed class CBMProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<CBMProvider> _logger;
	public CBMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBMProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var json = await Http.GetStringAsync(ApiUrl, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("rates", out var rates)) {
				return results;
			}

			var currencyCode = quoteCurrency;

			if (!rates.TryGetProperty(currencyCode, out var rateProp) || rateProp.ValueKind != JsonValueKind.String
				|| !decimal.TryParse(rateProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
				return results;
			}

			// "timestamp": "1791187200" (unix seconds)
			var date = doc.RootElement.TryGetProperty("timestamp", out var t) && long.TryParse(t.GetString(), out var seconds)
				? DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime)
				: DateOnly.FromDateTime(DateTime.UtcNow);

			// Currencies with small unit values are quoted in kyat per 100 units.
			if (PerHundredCurrencies.Contains(currencyCode)) {
				rate /= 100m;
			}

			if (date < fromDate || date > toDate) {
				return results;
			}

			results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBM rates.");
			return [];
		}
	}

	private static readonly HashSet<string> PerHundredCurrencies = new(StringComparer.OrdinalIgnoreCase) { "IDR", "JPY", "KHR", "KRW", "LAK", "VND" };
}
