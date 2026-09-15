
using ExchangeRates.Domain.Entities;

using HtmlAgilityPack;

using System.Globalization;
using System.Net;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central del Paraguay
/// </summary>
public sealed class BCPProvider : CentralBankProviderBase {
	private readonly ILogger<BCPProvider> _logger;
	public BCPProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCPProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		for (var year = fromDate.Year; year <= toDate.Year; year++) {

			var uri = Url + $"/xls?anho={year}&moneda={quoteCurrency}";
			var request = new HttpRequestMessage(HttpMethod.Get, uri);

			HttpResponseMessage response = await Http.SendAsync(request, ct);
			response.EnsureSuccessStatusCode();

			var html = await response.Content.ReadAsStringAsync(ct);
			var table = ExtractTable(html, quoteCurrency, year);
			results.AddRange(table);

		}
		return results;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="html"></param>
	/// <returns></returns>
	private IReadOnlyList<ExchangeRateResult> ExtractTable(string html, string quoteCurrency, int year) {

		var document = new HtmlDocument();
		document.LoadHtml(html);

		var tables = document.DocumentNode.SelectNodes("//table");

		if (tables is null || tables.Count < 2) {
			return [];
		}
		var rows = new List<ExchangeRateResult>();
		var nodes = tables[1].SelectNodes(".//tr").ToArray();

		for (var d = 1; d < nodes.Length - 2; d++) {

			var cells = nodes[d].SelectNodes("./th|./td");

			if (cells is null) {
				continue;
			}

			var values = cells.Select(cell => WebUtility.HtmlDecode(cell.InnerText).Trim())
														.ToList();

			for (var m = 1; m <= 12; m++) {
				if (d > DateTime.DaysInMonth(year, m)) {
					continue;
				}

				var value = values[m]?.Replace(',', '.');

				if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal rate)) {

					rows.Add(new ExchangeRateResult(new DateOnly(year, m, d), Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				}
			}
		}

		return rows;
	}
}
