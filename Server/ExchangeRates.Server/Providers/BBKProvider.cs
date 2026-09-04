using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Utilities;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Deutsche Bundesbank
/// </summary>
public sealed class BBKProvider : CentralBankProviderBase {
	private readonly ILogger<BBKProvider> _logger;
	public BBKProvider(HttpClient http, IConfiguration configuration, ILogger<BBKProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BBK";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url}?startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}&format=csvdata";

		var csv = await Http.GetStringAsync(url, ct);
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length < 2) {
			return [];
		}

		var headers = TextUtils.SplitCsv(lines[0]);
		var rates = new List<ExchangeRateResult>();

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
			var _quoteCurrency = currencyDenomIndex >= 0 && currencyDenomIndex < columns.Count ? columns[currencyDenomIndex] : null;

			if (string.IsNullOrWhiteSpace(baseCurrency) || string.IsNullOrWhiteSpace(_quoteCurrency)) {
				continue;
			}

			if (InverseProvider) {
				rate = 1m / rate;
			}

			rates.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
		}

		return rates;
	}

}
