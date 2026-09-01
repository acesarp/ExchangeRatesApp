using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Samoa
/// </summary>
public sealed class CBSProvider : CentralBankProviderBase {
	private readonly ILogger<CBSProvider> _logger;
	public CBSProvider(HttpClient http, IConfiguration configuration, ILogger<CBSProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBS";
	public override string Name => "Central Bank of Samoa";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.WST;

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var html = await Http.GetStringAsync(Url, ct);
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

				if (!decimal.TryParse(values[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(today, NativeCurrency, quoteCurrency, rate, Code));
				break;
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBS rates.");
			return [];
		}
	}
}
