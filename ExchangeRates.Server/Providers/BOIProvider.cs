using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Utilities;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Israel
/// </summary>
public sealed class BOIProvider : CentralBankProviderBase {
	public BOIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOI";
	public override string Name => "Bank of Israel";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.ILS;
	/// 
	/// <summary>
	/// Bank of Israel.
	/// Retrieves representative exchange rates against the Israeli Shekel (ILS).
	/// </summary>

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url.TrimEnd('/')}/" +
								$"?c%5BDATA_TYPE%5D=OF00" +
								$"&startperiod={fromDate:yyyy-MM-dd}" +
								$"&endperiod={toDate:yyyy-MM-dd}" +
								$"&format=csv" +
								$"&labels=id";

		var csv = await Http.GetStringAsync(url, ct);
		var rates = new List<ExchangeRate>();
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length < 2) {
			return rates;
		}

		var headers = TextUtils.SplitCsv(lines[0]);
		var dateIndex = headers.FindIndex(x => x.Equals("TIME_PERIOD", StringComparison.OrdinalIgnoreCase));

		foreach (var line in lines.Skip(1)) {
			var columns = TextUtils.SplitCsv(line);

			if (dateIndex < 0 || dateIndex >= columns.Count ||
				!DateOnly.TryParse(columns[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			var seriesCode = columns.FirstOrDefault(x => x.StartsWith("RER_", StringComparison.OrdinalIgnoreCase));

			if (seriesCode is null) {
				continue;
			}

			// Example:
			// RER_USD_ILS
			var parts = seriesCode.Split('_');

			if (parts.Length < 3) {
				continue;
			}

			var currency = parts[1];
			var rateText = columns.LastOrDefault(x => decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out _));

			if (rateText is null) {
				continue;
			}

			if (!decimal.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)) {
				continue;
			}

			if (rate <= 0) {
				continue;
			}
			rates.Add(new ExchangeRate(date, NativeCurrency, quoteCurrency, rate, Code));
		}
		return rates;
	}
}
