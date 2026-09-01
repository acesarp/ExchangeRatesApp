using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Utilities;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// European Central Bank
/// </summary>
public sealed class ECBProvider : CentralBankProviderBase {
	private readonly ILogger<ECBProvider> _logger;
	public ECBProvider(HttpClient http, IConfiguration configuration, ILogger<ECBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "ECB";
	public override string Name => "European Central Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.EUR;

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url}?startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}&format=csvdata";

		var csv = await Http.GetStringAsync(url, ct);
		var rates = new List<ExchangeRateResult>();
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length < 2) {
			return rates;
		}

		var headers = TextUtils.SplitCsv(lines[0]);
		var dateIndex = headers.FindIndex(x => x.Equals("TIME_PERIOD", StringComparison.OrdinalIgnoreCase));
		var currencyIndex = headers.FindIndex(x => x.Equals("OBS_VALUE", StringComparison.OrdinalIgnoreCase));
		lines = lines.Skip(1)
							.Where(w => w.Contains(quoteCurrency.ToString(), StringComparison.OrdinalIgnoreCase))
							.ToArray();
		foreach (var line in lines) {
			var cols = TextUtils.SplitCsv(line);
			if (dateIndex < 0 || dateIndex >= cols.Count || !DateOnly.TryParse(cols[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			if (decimal.TryParse(cols[currencyIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate > 0) {
				rates.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}
		}
		return rates;
	}
}
