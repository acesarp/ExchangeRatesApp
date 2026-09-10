
using HtmlAgilityPack;

using System.Globalization;
using System.Net;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Moldova
/// </summary>
public sealed class NBMProvider : CentralBankProviderBase {
	private readonly ILogger<NBMProvider> _logger;
	public NBMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBMProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var html = await Http.GetStringAsync(Url, ct);
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

				
					
				results.Add(new ExchangeRateResult(today, Bank.Currency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				break;
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch NBM rates.");
			return [];
		}
	}
}

