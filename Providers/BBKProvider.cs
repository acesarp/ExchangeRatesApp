using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Utilities;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Deutsche Bundesbank
/// </summary>
public sealed class BBKProvider : CentralBankProviderBase {
	public BBKProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BBK";
	public override string Name => "Deutsche Bundesbank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.DEM;
	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrencyISO fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url}?startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}&format=csvdata";

		var csv = await Http.GetStringAsync(url, ct);
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length < 2) {
			return [];
		}

		var headers = TextUtils.SplitCsv(lines[0]);
		var rates = new List<ExchangeRate>();

		var dateIndex = headers.FindIndex(x => x.Equals("TIME_PERIOD", StringComparison.OrdinalIgnoreCase));
		var valueIndex = headers.FindIndex(x => x.Equals("OBS_VALUE", StringComparison.OrdinalIgnoreCase));
		var currencyIndex = headers.FindIndex(x => x.Equals("CURRENCY", StringComparison.OrdinalIgnoreCase));
		var currencyDenomIndex = headers.FindIndex(x => x.Equals("CURRENCY_DENOM", StringComparison.OrdinalIgnoreCase));

		for (var i = 1; i < lines.Length; i++) {
			var columns = TextUtils.SplitCsv(lines[i]);

			if (dateIndex < 0 || valueIndex < 0 || dateIndex >= columns.Count || valueIndex >= columns.Count) {
				continue;
			}

			if (!DateOnly.TryParse(columns[dateIndex], CultureInfo.InvariantCulture, out var date)) {
				continue;
			}

			if (!decimal.TryParse(columns[valueIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
				continue;
			}

			var baseCurrency = currencyIndex >= 0 && currencyIndex < columns.Count ? columns[currencyIndex] : null;
			var quoteCurrency = currencyDenomIndex >= 0 && currencyDenomIndex < columns.Count ? columns[currencyDenomIndex] : null;

			if (string.IsNullOrWhiteSpace(baseCurrency) || string.IsNullOrWhiteSpace(quoteCurrency)) {
				continue;
			}

			rates.Add(new ExchangeRate(date, Enum.Parse<ECurrencyISO>(baseCurrency), Enum.Parse<ECurrencyISO>(quoteCurrency), rate, Code));
		}

		return rates;
	}

}
