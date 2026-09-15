using HtmlAgilityPack;

using Microsoft.Playwright;

using Newtonsoft.Json;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// International Monetary Fund
/// </summary>
public sealed class IMFProvider : CentralBankProviderBase {
	private readonly ILogger<IMFProvider> _logger;

	public IMFProvider(HttpClient http, IConfiguration configuration, ILogger<IMFProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	/// <summary>
	/// Fetches exchange rate data from the IMF API for the specified quote currency and date range.
	/// </summary>
	protected async Task<IReadOnlyList<ExchangeRateResult>> FetchMQAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		const string context = "dataflow";
		const string agencyID = "IMF.STA";
		const string resourceID = "ER";
		const string version = "+";
		const string indicator = "XDC_XDR";
		const string transformation = "PA_RT";
		const string frequency = "M";
		var fromPeriod = $"{fromDate.Year}-M{fromDate.Month:00}-M{fromDate.Day:00}";
		var toPeriod = $"{toDate.Year}-M{toDate.Month:00}-M{toDate.Day:00}";
		var country = quoteCurrency; //WRONG TODO
		var key = $"{country}.{indicator}.{transformation}.{frequency}";


		var uri = $"{Url.TrimEnd('/')}/{context}/{agencyID}/{resourceID}/{version}/{key}" +
			$"?c[TIME_PERIOD]=ge:{fromPeriod}%2Ble:{toPeriod}" +
			"&dimensionAtObservation=TIME_PERIOD" +
			"&attributes=dsd" +
			"&measures=all" +
			"&includeHistory=false";

		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		request.Headers.Accept.ParseAdd("application/vnd.sdmx.data+json;version=2.0.0");
		using var response = await Http.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();

		var json = await response.Content.ReadAsStringAsync(ct);
		return JsonConvert.DeserializeObject<IReadOnlyList<ExchangeRateResult>>(json);
	}

	/// <summary>
	/// Fetches exchange rate data from the IMF API for the specified quote currency and date range.
	/// </summary>
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var results = new List<ExchangeRateResult>();
		if (quoteCurrency is null) {
			_logger.LogDebug("NativeCurrency {NativeCurrency} is not supported by IMF.", quoteCurrency);
			return [];
		}

		for (var month = new DateOnly(fromDate.Year, fromDate.Month, 1); month <= toDate; month = month.AddMonths(1)) {

			var selectDate = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
			var url = $"{Url}?SelectDate={selectDate:yyyy-MM-dd}&reportType=CVSDR";

			using var playwright = await Playwright.CreateAsync();
			await using var browser = await playwright.Chromium.LaunchAsync(new() {
				Headless = true
			});

			var page = await browser.NewPageAsync();
			await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });

			var html = await page.ContentAsync();

			var document = new HtmlDocument();
			document.LoadHtml(html);

			var tables = document.DocumentNode.SelectNodes("//table");

			if (tables is null) {
				continue;
			}

			foreach (var table in tables) {
				var rows = table.SelectNodes(".//tr");

				if (rows is null || rows.Count < 2) {
					continue;
				}

				var dates = ParseDates(rows, month.Year);

				if (dates.Count == 0) {
					continue;
				}

				foreach (var row in rows) {
					var cells = row.SelectNodes("./th|./td");

					if (cells is null || cells.Count < 2) {
						continue;
					}

					var name = Clean(cells[0].InnerText);

					if (!name.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					for (var i = 1; i < cells.Count && i <= dates.Count; i++) {
						var date = dates[i - 1];

						if (date < fromDate || date > toDate) {
							continue;
						}

						var text = Clean(cells[i].InnerText);

						if (text.Equals("NA", StringComparison.OrdinalIgnoreCase)) {
							continue;
						}

						text = text.Replace(",", "");

						if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate)) {
							continue;
						}

						results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
					}
				}
			}
		}

		return results.OrderBy(x => x.Date).ToList();
	}

	private static List<DateOnly> ParseDates(IEnumerable<HtmlNode> rows, int year) {
		foreach (var row in rows) {
			var cells = row.SelectNodes("./th|./td");

			if (cells is null || cells.Count < 2) {
				continue;
			}

			if (!Clean(cells[0].InnerText).Equals("NativeCurrency", StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			var dates = new List<DateOnly>();

			for (var i = 1; i < cells.Count; i++) {
				var text = Clean(cells[i].InnerText);

				if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date)) {
					dates.Add(DateOnly.FromDateTime(date));
					continue;
				}

				if (DateTime.TryParse($"{text} {year}", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date)) {
					dates.Add(DateOnly.FromDateTime(date));
				}
			}

			if (dates.Count > 0) {
				return dates;
			}
		}

		return [];
	}


	private static string Clean(string value) {
		return string.Join(" ", HtmlEntity.DeEntitize(value).Replace("\r", " ")
																		.Replace("\n", " ")
																		.Replace("\t", " ")
																		.Trim()
																		.Split(' ', StringSplitOptions.RemoveEmptyEntries));
	}

}
