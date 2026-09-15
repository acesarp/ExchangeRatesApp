using ExchangeRates.Domain.Entities;
using ExchangeRates.Server.Utilities;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// European Central Bank
/// </summary>
public sealed class ECBProvider : CentralBankProviderBase {
	private readonly ILogger<ECBProvider> _logger;
	public ECBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<ECBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url}/D.{quoteCurrency}.EUR.SP00.A?startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}&format=csvdata";

		_logger.LogDebug("ECB request: {Url}", url);

		var csv = await Http.GetStringAsync(url, ct);

		if (string.IsNullOrWhiteSpace(csv)) {
			return Array.Empty<ExchangeRateResult>();
		}

		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (lines.Length < 2) {
			return Array.Empty<ExchangeRateResult>();
		}

		var headers = TextUtils.SplitCsv(lines[0]);
		var dateIndex = headers.FindIndex(x => x.Equals("TIME_PERIOD", StringComparison.OrdinalIgnoreCase));
		var valueIndex = headers.FindIndex(x => x.Equals("OBS_VALUE", StringComparison.OrdinalIgnoreCase));

		if (dateIndex < 0 || valueIndex < 0) {
			_logger.LogWarning("ECB CSV response does not contain TIME_PERIOD or OBS_VALUE columns.");
			return Array.Empty<ExchangeRateResult>();
		}

		var rates = new List<ExchangeRateResult>();

		foreach (var line in lines.Skip(1)) {
			var cols = TextUtils.SplitCsv(line);

			if (dateIndex >= cols.Count || valueIndex >= cols.Count) {
				continue;
			}

			if (!DateOnly.TryParse(cols[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			if (!decimal.TryParse(cols[valueIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
		}

		return rates.OrderBy(x => x.Date)
							.ToList();
	}


	protected async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync_OLD(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
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
							.Where(w => w.Contains(quoteCurrency, StringComparison.OrdinalIgnoreCase))
							.ToArray();
		foreach (var line in lines) {
			var cols = TextUtils.SplitCsv(line);
			if (dateIndex < 0 || dateIndex >= cols.Count || !DateOnly.TryParse(cols[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			if (decimal.TryParse(cols[currencyIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate > 0) {


				rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}
		}
		return rates;
	}
}

