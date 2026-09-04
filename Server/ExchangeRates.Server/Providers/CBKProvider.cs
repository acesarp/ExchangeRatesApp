using ExchangeRates.Domain.Enums;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Kenya
/// </summary>
public sealed class CBKProvider : CentralBankProviderBase {
	private readonly ILogger<CBKProvider> _logger;

	public CBKProvider(HttpClient http, IConfiguration configuration, ILogger<CBKProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBK";
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			using var content = new FormUrlEncodedContent(new Dictionary<string, string> {
				["action"] = "get_indicative_fx_rates"
			});

			using var response = await Http.PostAsync(Url, content, ct);

			if (!response.IsSuccessStatusCode) {
				return [];
			}

			var html = await response.Content.ReadAsStringAsync(ct);
			var doc = new HtmlAgilityPack.HtmlDocument();
			doc.LoadHtml(html);

			var results = new List<ExchangeRateResult>();
			var rows = doc.DocumentNode.SelectNodes("//table//tr");

			if (rows is null) {
				return results;
			}

			var currencyCode = quoteCurrency.ToString();
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

				if (!values[0].Contains(currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!decimal.TryParse(values[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				if (InverseProvider) {
					rate = 1m / rate;
				}
				results.Add(new ExchangeRateResult(today, NativeCurrency, quoteCurrency, rate, Code));
				break;
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBK rates.");
			return [];
		}
	}
}
