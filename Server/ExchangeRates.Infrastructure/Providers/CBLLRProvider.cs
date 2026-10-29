
using System.Globalization;

using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Central Bank of Liberia
/// </summary>
public sealed class CBLLRProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<CBLLRProvider> _logger;

	public CBLLRProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBLLRProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		// The page only publishes LRD/USD buying and selling rates.
		if (quoteCurrency != "USD") {
			return [];
		}

		try {
			var html = await Http.GetStringAsync(ApiUrl, ct);
			var doc = new HtmlAgilityPack.HtmlDocument();
			doc.LoadHtml(html);

			var results = new List<ExchangeRateResult>();
			var rows = doc.DocumentNode.SelectNodes("//table//tr");

			if (rows is null) {
				return results;
			}

			// | Monday, October 5, 2026 | L$170.1186/US$1.00 | L$172.1587/US$1.00 |
			foreach (var row in rows) {
				var cells = row.SelectNodes("./td");

				if (cells is null || cells.Count < 3) {
					continue;
				}

				var values = cells.Select(c => System.Net.WebUtility.HtmlDecode(c.InnerText).Trim()).ToArray();

				if (!DateOnly.TryParseExact(values[0], "dddd, MMMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				if (!TryParseRate(values[1], out var buying) || !TryParseRate(values[2], out var selling)) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, (buying + selling) / 2m, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBLLR rates.");
			return [];
		}
	}

	/// <summary>Parses "L$170.1186/US$1.00" into 170.1186.</summary>
	private static bool TryParseRate(string text, out decimal rate) {
		var amount = text.Split('/')[0].Replace("L$", string.Empty).Trim();
		return decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out rate) && rate > 0;
	}
}

