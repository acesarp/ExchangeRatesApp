using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Utilities;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of England.
/// Retrieves daily spot exchange rates against Pound Sterling (GBP).
/// </summary>
public sealed class BOEProvider : CentralBankProviderBase {
	public BOEProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }
	private static readonly Dictionary<string, string> Series = new() {
		// Current / recent currencies
		["AUD"] = "XUDLADS",   // Australian Dollar
		["BGN"] = "XUDLZOS3",  // Bulgarian Lev
		["CAD"] = "XUDLCDS",   // Canadian Dollar
		["CNY"] = "XUDLBK89",  // Chinese Yuan
		["CYP"] = "XUDLBK22",  // Cyprus Pound
		["CZK"] = "XUDLBK25",  // Czech Koruna
		["DKK"] = "XUDLDKS",   // Danish Krone
		["EEK"] = "XUDLBK28",  // Estonian Kroon
		["EUR"] = "XUDLERS",   // Euro
		["HKD"] = "XUDLHDS",   // Hong Kong Dollar
		["JPY"] = "XUDLJYS",   // Japanese Yen
		["HUF"] = "XUDLBK33",  // Hungarian Forint
		["INR"] = "XUDLBK97",  // Indian Rupee
		["LVL"] = "XUDLBK39",  // Latvian Lats
		["ILS"] = "XUDLBK78",  // Israeli Shekel
		["LTL"] = "XUDLBK36",  // Lithuanian Litas
		["MYR"] = "XUDLBK83",  // Malaysian Ringgit
		["MTL"] = "XUDLBK44",  // Maltese Lira
		["NZD"] = "XUDLNDS",   // New Zealand Dollar
		["NOK"] = "XUDLNKS",   // Norwegian Krone
		["PLN"] = "XUDLBK47",  // Polish Zloty
		["RON"] = "XUDLZOS4",  // Romanian Leu
		["RUB"] = "XUDLBK68",  // Russian Ruble
		["SAR"] = "XUDLSRS",   // Saudi Riyal
		["SGD"] = "XUDLSGS",   // Singapore Dollar
		["SKK"] = "XUDLBK55",  // Slovak Koruna
		["SEK"] = "XUDLSKS",   // Swedish Krona
		["CHF"] = "XUDLSFS",   // Swiss Franc
		["SIT"] = "XUDLBK52",  // Slovenian Tolar
		["ZAR"] = "XUDLZRS",   // South African Rand
		["KRW"] = "XUDLBK93",  // South Korean Won
		["TWD"] = "XUDLTWS",   // Taiwan Dollar
		["THB"] = "XUDLBK87",  // Thai Baht
		["TRY"] = "XUDLBK95",  // Turkish Lira
		["USD"] = "XUDLUSS",   // US Dollar

		// Legacy pre-Euro currencies
		["ATS"] = "XUDLASS",   // Austrian Schilling
		["BEF"] = "XUDLBFS",   // Belgian Franc
		["DEM"] = "XUDLDMS",   // Deutschemark
		["GRD"] = "XUDLGDS",   // Greek Drachma
		["FIM"] = "XUDLFMS",   // Finnish Markka
		["FRF"] = "XUDLFFS",   // French Franc
		["IEP"] = "XUDLIPS",   // Irish Punt
		["ITL"] = "XUDLILS",   // Italian Lira
		["NLG"] = "XUDLNGS",   // Netherlands Guilder
		["PTE"] = "XUDLPES",   // Portuguese Escudo
		["ESP"] = "XUDLSPS"    // Spanish Peseta
	};
	public override string Code => "BOE";
	public override string Name => "Bank of England";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.GBP;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (!Series.TryGetValue(quoteCurrency.ToString(), out var seriesCode)) {
			return [];
		}

		var url = $"{Url}?csv.x=yes" +
			$"&Datefrom={Uri.EscapeDataString($"{fromDate:dd/MMM/yyyy}")}" +
			$"&Dateto={Uri.EscapeDataString($"{toDate:dd/MMM/yyyy}")}" +
			$"&SeriesCodes={seriesCode}" +
			"&UsingCodes=Y" +
			"&CSVF=TN";

		var csv = await Http.GetStringAsync(url, ct);
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length < 2) {
			return [];
		}

		var headers = TextUtils.SplitCsv(lines[0]);
		var rates = new List<ExchangeRate>();
		var dateIndex = headers.FindIndex(x => x.Equals("DATE", StringComparison.OrdinalIgnoreCase));
		var valueIndex = headers.FindIndex(x => x.Equals(seriesCode, StringComparison.OrdinalIgnoreCase));

		foreach (var line in lines.Skip(1)) {
			var values = TextUtils.SplitCsv(line);
			if (dateIndex < 0 || valueIndex < 0 || dateIndex >= values.Count || valueIndex >= values.Count) {
				continue;
			}

			if (!DateOnly.TryParse(values[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
				!decimal.TryParse(values[valueIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
				rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRate(date, NativeCurrency, quoteCurrency, rate, Code));
		}
		return rates;
	}
}
