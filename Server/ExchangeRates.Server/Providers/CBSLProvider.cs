using ExchangeRates.Domain.Enums;

using HtmlAgilityPack;

using System.Globalization;
using System.Net;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Sri Lanka
/// </summary>
public sealed class CBSLProvider : CentralBankProviderBase {
	private readonly ILogger<CBSLProvider> _logger;

	public CBSLProvider(HttpClient http, IConfiguration configuration, ILogger<CBSLProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBSL";
	public override string Name => "Central Bank of Sri Lanka";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.LKR;

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var html = await Http.GetStringAsync(Url, ct);
			var document = new HtmlDocument();
			document.LoadHtml(html);

			var results = new List<ExchangeRateResult>();
			var rows = document.DocumentNode.SelectNodes("//table//tr");

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

				if (cells is null || cells.Count < 3) {
					continue;
				}

				var values = cells.Select(c => WebUtility.HtmlDecode(c.InnerText).Trim()).ToArray();

				if (!values[0].Contains(currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!decimal.TryParse(values[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(today, NativeCurrency, quoteCurrency, rate, Code));
				break;
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBSL rates.");
			return [];
		}
	}
}
