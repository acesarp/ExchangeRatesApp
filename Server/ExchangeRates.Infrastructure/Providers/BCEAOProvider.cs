
using ExchangeRates.Domain.Models;
using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Text.Json;
using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Banque Centrale des Etats de l'Afrique de l'Ouest
/// </summary>
public sealed class BCEAOProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BCEAOProvider> _logger;

	public BCEAOProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCEAOProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		// The endpoint returns an HTML fragment listing currencies by French name: <tr><td>Dollar us</td><td>580,6000</td></tr>
		var currencyName = CurrencyNames.FirstOrDefault(x => string.Equals(x.Value, quoteCurrency, StringComparison.OrdinalIgnoreCase)).Key;

		if (currencyName is null) {
			return results;
		}

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) {
				continue;
			}

			try {
				var html = await Http.GetStringAsync($"{ApiUrl}?dateJour={date:yyyy-MM-dd}", ct);
				var document = new HtmlAgilityPack.HtmlDocument();
				document.LoadHtml(html);

				var rows = document.DocumentNode.SelectNodes("//tr[td]");

				if (rows is null) {
					continue;
				}

				foreach (var row in rows) {
					var cells = row.SelectNodes("./td");

					if (cells is null || cells.Count < 2) {
						continue;
					}

					var name = System.Net.WebUtility.HtmlDecode(cells[0].InnerText).Trim();

					if (!string.Equals(name, currencyName, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					// French number format: "1.885,3550"
					var value = System.Net.WebUtility.HtmlDecode(cells[1].InnerText).Trim().Replace(".", string.Empty).Replace(',', '.');

					if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) && rate > 0) {
						results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
					}

					break;
				}
			}
			catch (Exception ex) when (ex is not OperationCanceledException) {
				_logger.LogWarning(ex, "Failed to fetch BCEAO rate for {Date}", date);
			}
		}

		return results;
	}

	private static readonly Dictionary<string, string> CurrencyNames = new(StringComparer.OrdinalIgnoreCase) {
		["Euro"] = "EUR",
		["Dollar us"] = "USD",
		["Yen japonais"] = "JPY",
		["Couronne danoise"] = "DKK",
		["Couronne suédoise"] = "SEK",
		["Livre sterling"] = "GBP",
		["Couronne norvégienne"] = "NOK",
		["Couronne thèque"] = "CZK",
		["Forint hongrois"] = "HUF",
		["Zloty polonais"] = "PLN",
		["Franc suisse"] = "CHF",
		["Dollar canadien"] = "CAD",
		["Dollar australien"] = "AUD",
		["Dollar néo-zélandais"] = "NZD",
		["Rand sud-africain"] = "ZAR",
		["Yuan chinois"] = "CNY",
		["Roupie Indienne"] = "INR",
		["Baht thailandais"] = "THB",
		["Real brésilien"] = "BRL",
		["Dollar singapourien"] = "SGD",
		["Nouvelle livre turque"] = "TRY",
		["Dirham Emirats Arabes Unis"] = "AED",
		["Nouveau Shekel"] = "ILS",
		["Won Coréen"] = "KRW",
		["Dollar Hong Kong"] = "HKD",
		["Ryal Saudien"] = "SAR",
		["Dinar Koweitien"] = "KWD"
	};
}

