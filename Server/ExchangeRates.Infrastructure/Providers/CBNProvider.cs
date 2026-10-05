
using ExchangeRates.Domain.Entities;

using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Central Bank of Nigeria
/// </summary>
public sealed class CBNProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<CBNProvider> _logger;

	public CBNProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBNProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			// The CBN feed identifies currencies by name, not ISO code.
			var currencyName = CurrencyNames.FirstOrDefault(x => string.Equals(x.Value, quoteCurrency, StringComparison.OrdinalIgnoreCase)).Key;

			if (currencyName is null) {
				return [];
			}

			// Returns the full history: [{ "currency": "US DOLLAR", "ratedate": "2026-10-05", "centralrate": "1331.1875", ... }]
			var json = await Http.GetStringAsync(ApiUrl, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (doc.RootElement.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in doc.RootElement.EnumerateArray()) {
				var name = item.TryGetProperty("currency", out var c) ? c.GetString()?.Trim() : null;

				if (!string.Equals(name, currencyName, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!item.TryGetProperty("ratedate", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "centralrate");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBN rates.");
			return [];
		}
	}

	private static readonly Dictionary<string, string> CurrencyNames = new(StringComparer.OrdinalIgnoreCase) {
		["US DOLLAR"] = "USD",
		["EURO"] = "EUR",
		["POUNDS STERLING"] = "GBP",
		["YEN"] = "JPY",
		["SWISS FRANC"] = "CHF",
		["YUAN/RENMINBI"] = "CNY",
		["DANISH KRONA"] = "DKK",
		["SOUTH AFRICAN RAND"] = "ZAR",
		["UAE DIRHAM"] = "AED",
		["RIYAL"] = "SAR",
		["CFA"] = "XOF",
		["SDR"] = "XDR"
	};
}

