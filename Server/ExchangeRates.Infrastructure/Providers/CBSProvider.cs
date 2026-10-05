
using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Central Bank of Samoa
/// </summary>
public sealed class CBSProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.ForeignPerNative;

	private readonly ILogger<CBSProvider> _logger;
	public CBSProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBSProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var html = await Http.GetStringAsync(ApiUrl, ct);
			var doc = new HtmlAgilityPack.HtmlDocument();
			doc.LoadHtml(html);

			var results = new List<ExchangeRateResult>();
			var rows = doc.DocumentNode.SelectNodes("//table//tr");

			if (rows is null) {
				return results;
			}

			var currencyCode = quoteCurrency;
			var today = DateOnly.FromDateTime(DateTime.UtcNow);

			if (today < fromDate || today > toDate) {
				return results;
			}

			foreach (var row in rows) {
				var cells = row.SelectNodes("./td");

				if (cells is null || cells.Count < 2) {
					continue;
				}

				var values = cells.Select(c => System.Net.WebUtility.HtmlDecode(c.InnerText).Trim()).ToArray();

				// Currencies are listed by name, e.g. "United States Dollar | 0.36045" (foreign units per tala).
				if (!CurrencyNames.TryGetValue(values[0], out var rowCurrency) || !string.Equals(rowCurrency, currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!decimal.TryParse(values[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				
					
				results.Add(new ExchangeRateResult(today, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				break;
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBS rates.");
			return [];
		}
	}

	private static readonly Dictionary<string, string> CurrencyNames = new(StringComparer.OrdinalIgnoreCase) {
		["United States Dollar"] = "USD",
		["New Zealand Dollar"] = "NZD",
		["Australian Dollar"] = "AUD",
		["Euro"] = "EUR",
		["Fijian Dollar"] = "FJD",
		["Japanese Yen"] = "JPY",
		["Great Britain Pound"] = "GBP",
		["Chinese Yuan Onshore"] = "CNY"
	};
}
