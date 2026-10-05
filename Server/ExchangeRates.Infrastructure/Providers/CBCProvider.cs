
using System.Globalization;
using System.Text.Json;

using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Central Bank of the Republic of China (Taiwan)
/// </summary>
public sealed class CBCProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<CBCProvider> _logger;
	public CBCProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBCProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency != "USD") {
			return [];
		}

		try {
			var json = await Http.GetStringAsync(ApiUrl, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			// { "data": { "structure": { "Table1": [{ "data": "NTD/USD" }, ...] }, "dataSets": [["20260930", "31.852", ...], ...] } }
			// Column 0 is the date (yyyyMMdd), column 1 is NTD per USD.
			if (!doc.RootElement.TryGetProperty("data", out var data) ||
				!data.TryGetProperty("dataSets", out var dataSets) || dataSets.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in dataSets.EnumerateArray()) {
				if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 2) {
					continue;
				}

				if (!DateOnly.TryParseExact(item[0].GetString(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				if (!decimal.TryParse(item[1].GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBC rates.");
			return [];
		}
	}
}

