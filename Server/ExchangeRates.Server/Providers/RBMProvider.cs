
using ExchangeRates.Domain.Entities;

using HtmlAgilityPack;

using System.Globalization;
using System.Net;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Malawi
/// </summary>
public sealed class RBMProvider : CentralBankProviderBase {
	private readonly ILogger<RBMProvider> _logger;
	public RBMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<RBMProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var html = await Http.GetStringAsync(ApiUrl, ct);
			var document = new HtmlDocument();
			document.LoadHtml(html);

			var results = new List<ExchangeRateResult>();
			var rows = document.DocumentNode.SelectNodes("//table//tr");

			if (rows is null) {
				return results;
			}

			var currencyCode = quoteCurrency;
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


				results.Add(new ExchangeRateResult(today, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				break;
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch RBM rates.");
			return [];
		}
	}
}

