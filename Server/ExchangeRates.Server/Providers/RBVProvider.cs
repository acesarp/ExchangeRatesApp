using ExchangeRates.Domain.Enums;

using HtmlAgilityPack;

using System.Globalization;
using System.Net;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Vanuatu
/// </summary>
public sealed class RBVProvider : CentralBankProviderBase {
	private readonly ILogger<RBVProvider> _logger;

	public RBVProvider(HttpClient http, IConfiguration configuration, ILogger<RBVProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "RBV";
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

				if (cells is null || cells.Count < 2) {
					continue;
				}

				var values = cells.Select(c => WebUtility.HtmlDecode(c.InnerText).Trim()).ToArray();

				if (!values[0].Contains(currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!decimal.TryParse(values[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				
					
				results.Add(new ExchangeRateResult(today, NativeCurrency, quoteCurrency, rate, Code));
				break;
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch RBV rates.");
			return [];
		}
	}
}
