using ExchangeRates.Domain.Entities;
using ExchangeRates.Server.Utilities;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Autoritat Financera Andorrana (Andorran Financial Authority) - https://www.afa.ad/en/ <br />
/// Andorra has no independent monetary policy and uses the Euro (EUR) as its official currency,<br />
/// so this provider mirrors the same CSV data endpoint contract as <see cref="ECBProvider"/>.
/// </summary>
public sealed class AFAProvider : CentralBankProviderBase {
	private readonly ILogger<AFAProvider> _logger;
	public AFAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<AFAProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{ApiUrl}?startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}&format=csvdata";

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

