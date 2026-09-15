
using ExchangeRates.Domain.Entities;

using HtmlAgilityPack;

using System.Globalization;
using System.Net;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank Indonesia
/// </summary>
public sealed class BIProvider : CentralBankProviderBase {
	private readonly ILogger<BIProvider> _logger;

	public BIProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BIProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var uri = $"{Url}?tanggal={date:dd/MM/yyyy}";
				using var response = await Http.GetAsync(uri, ct);

				if (!response.IsSuccessStatusCode) {
					continue;
				}

				var html = await response.Content.ReadAsStringAsync(ct);
				var rate = ExtractRate(html, quoteCurrency);

				if (rate is null || rate <= 0) {
					continue;
				}



				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate.Value, Bank.BankCode));
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to fetch BI rate for {Date}", date);
			}
		}

		return results;
	}

	private static decimal? ExtractRate(string html, string quoteCurrency) {
		var document = new HtmlDocument();
		document.LoadHtml(html);

		var rows = document.DocumentNode.SelectNodes("//table//tr");

		if (rows is null) {
			return null;
		}

		var currencyCode = quoteCurrency;

		foreach (var row in rows) {
			var cells = row.SelectNodes("./td");

			if (cells is null || cells.Count < 3) {
				continue;
			}

			var values = cells.Select(cell => WebUtility.HtmlDecode(cell.InnerText).Trim()).ToArray();

			if (!values[0].Equals(currencyCode, StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			if (!decimal.TryParse(values[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var buy)) {
				continue;
			}

			if (!decimal.TryParse(values[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var sell)) {
				continue;
			}

			return (buy + sell) / 2m;
		}

		return null;
	}
}


