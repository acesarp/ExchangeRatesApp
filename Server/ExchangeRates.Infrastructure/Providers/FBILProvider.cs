
using ExchangeRates.Domain.Models;
using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Globalization;
using System.Text.Json;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary> Financial Benchmarks India </summary>
public sealed class FBILProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<FBILProvider> _logger;

	public FBILProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<FBILProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var uri = $"{ApiUrl}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&authenticated=false";
		using var response = await Http.GetAsync(uri, ct);
		response.EnsureSuccessStatusCode();
		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		var results = new List<ExchangeRateResult>();

		if (doc.RootElement.ValueKind != JsonValueKind.Array) {
			return results;
		}

		// [{ "processRunDate": "2026-09-28 00:00:00", "subProdName": "INR / 1 USD", "rate": 95.9681 }, ...]
		foreach (var item in doc.RootElement.EnumerateArray()) {
			if (!TryGetCurrency(item.TryGetProperty("subProdName", out var name) ? name.GetString() : null, out var currency, out var unitMultiplier) ||
				!string.Equals(currency, quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			if (!item.TryGetProperty("processRunDate", out var dateProp) ||
				!DateTime.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime)) {
				continue;
			}

			var date = DateOnly.FromDateTime(dateTime);

			if (date < fromDate || date > toDate) {
				continue;
			}

			var rate = GetDecimal(item, "rate");

			if (rate <= 0) {
				continue;
			}

			results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate / unitMultiplier, Bank.BankCode));
		}

		return results;
	}

	/// <summary>
	/// Parses FBIL product names such as "INR / 1 USD" or "INR / 100 JPY" into the currency and its quotation unit.
	/// </summary>
	private static bool TryGetCurrency(string? currencyPair, out string currency, out decimal multiplier) {
		currency = string.Empty;
		multiplier = 1m;

		var parts = currencyPair?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		// ["INR", "/", "100", "JPY"]
		if (parts is not { Length: 4 } || parts[1] != "/" || !decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out multiplier) || multiplier <= 0) {
			return false;
		}

		currency = parts[3];
		return true;
	}
}
