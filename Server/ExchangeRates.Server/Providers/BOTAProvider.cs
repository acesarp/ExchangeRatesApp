using ExchangeRates.Domain.Enums;

using HtmlAgilityPack;

using System.Globalization;
using System.Net;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Tanzania
/// </summary>
public sealed class BOTAProvider : CentralBankProviderBase {
	private readonly ILogger<BOTAProvider> _logger;

	public BOTAProvider(HttpClient http, IConfiguration configuration, ILogger<BOTAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOTA";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			using var response = await Http.GetAsync(Url, ct);

			if (!response.IsSuccessStatusCode) {
				return [];
			}

			var html = await response.Content.ReadAsStringAsync(ct);
			var document = new HtmlDocument();
			document.LoadHtml(html);

			var results = new List<ExchangeRateResult>();
			var rows = document.DocumentNode.SelectNodes("//table//tr");

			if (rows is null) {
				return results;
			}

			var currencyCode = quoteCurrency.ToString();

			foreach (var row in rows) {
				var cells = row.SelectNodes("./td");

				if (cells is null || cells.Count < 3) {
					continue;
				}

				var values = cells.Select(c => WebUtility.HtmlDecode(c.InnerText).Trim()).ToArray();

				if (!values[0].Equals(currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!decimal.TryParse(values[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				var today = DateOnly.FromDateTime(DateTime.UtcNow);

				if (today < fromDate || today > toDate) {
					continue;
				}

				results.Add(new ExchangeRateResult(today, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BOTA rates.");
			return [];
		}
	}
}
