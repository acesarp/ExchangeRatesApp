
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using HtmlAgilityPack;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Bank Indonesia
/// </summary>
public sealed class BIProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BIProvider> _logger;

	public BIProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BIProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		// The page ignores date query parameters (historical rates need an ASP.NET postback) and always shows the latest
		// publication, so only that day can be returned. Its date is in the "Last Update 5 October 2026" label.
		try {
			using var response = await Http.GetAsync(ApiUrl, ct);

			if (!response.IsSuccessStatusCode) {
				return [];
			}

			var document = new HtmlDocument();
			document.LoadHtml(await response.Content.ReadAsStringAsync(ct));

			var text = WebUtility.HtmlDecode(document.DocumentNode.InnerText);
			var match = Regex.Match(text, @"Last Update\s+(\d{1,2} \w+ \d{4})");

			if (!match.Success || !DateOnly.TryParseExact(match.Groups[1].Value, "d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				return [];
			}

			if (date < fromDate || date > toDate) {
				return [];
			}

			var rate = ExtractRate(document, quoteCurrency);

			if (rate is null || rate <= 0) {
				return [];
			}

			return [new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate.Value, Bank.BankCode)];
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BI rates.");
			return [];
		}
	}

	/// <summary>
	/// Reads the mid rate from the row | USD | 1 | 17,987.49 (sell) | 17,808.51 (buy) | (rupiah per unit).
	/// </summary>
	private static decimal? ExtractRate(HtmlDocument document, string quoteCurrency) {
		var rows = document.DocumentNode.SelectNodes("//table//tr");

		if (rows is null) {
			return null;
		}

		foreach (var row in rows) {
			var cells = row.SelectNodes("./td");

			if (cells is null || cells.Count < 4) {
				continue;
			}

			var values = cells.Select(cell => WebUtility.HtmlDecode(cell.InnerText).Trim()).ToArray();

			if (!values[0].Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			if (!decimal.TryParse(values[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var unit) || unit <= 0 ||
				!decimal.TryParse(values[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var sell) ||
				!decimal.TryParse(values[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var buy)) {
				continue;
			}

			return (sell + buy) / 2m / unit;
		}

		return null;
	}
}


