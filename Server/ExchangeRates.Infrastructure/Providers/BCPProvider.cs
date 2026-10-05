
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using HtmlAgilityPack;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Globalization;
using System.Net;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Banco Central del Paraguay
/// </summary>
public sealed class BCPProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BCPProvider> _logger;
	public BCPProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCPProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		for (var year = fromDate.Year; year <= toDate.Year; year++) {

			var uri = ApiUrl + $"/xls?anho={year}&moneda={quoteCurrency}";
			var request = new HttpRequestMessage(HttpMethod.Get, uri);

			HttpResponseMessage response = await Http.SendAsync(request, ct);
			response.EnsureSuccessStatusCode();

			var html = await response.Content.ReadAsStringAsync(ct);
			var table = ExtractTable(html, quoteCurrency, year);
			results.AddRange(table.Where(r => r.Date >= fromDate && r.Date <= toDate));

		}
		return results;
	}

	/// <summary>
	/// Parses the yearly rate sheet: one row per day of month (first cell), one column per month (ENE..DIC).
	/// Values use Paraguayan formatting ("6.486,59"); days without a rate are marked "ND".
	/// </summary>
	private IReadOnlyList<ExchangeRateResult> ExtractTable(string html, string quoteCurrency, int year) {
		var document = new HtmlDocument();
		document.LoadHtml(html);

		var rowNodes = document.DocumentNode.SelectNodes("//table//tr");

		if (rowNodes is null) {
			return [];
		}

		var rows = new List<ExchangeRateResult>();

		foreach (var rowNode in rowNodes) {
			var cells = rowNode.SelectNodes("./th|./td");

			if (cells is null || cells.Count < 13) {
				continue;
			}

			var values = cells.Select(cell => WebUtility.HtmlDecode(cell.InnerText).Trim()).ToList();

			if (!int.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out var day) || day is < 1 or > 31) {
				continue;
			}

			for (var month = 1; month <= 12; month++) {
				if (day > DateTime.DaysInMonth(year, month)) {
					continue;
				}

				var value = values[month].Replace(".", string.Empty).Replace(',', '.');

				if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) && rate > 0) {
					rows.Add(new ExchangeRateResult(new DateOnly(year, month, day), Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				}
			}
		}

		return rows;
	}
}
