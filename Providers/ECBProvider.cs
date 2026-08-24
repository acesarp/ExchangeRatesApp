using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// European Central Bank
/// </summary>
public sealed class ECBProvider : CentralBankProviderBase {
	public ECBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "ECB";
	public override string Name => "European Central Bank";
	public override ECurrency NativeCurrency => ECurrency.EUR;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url}?startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}&format=csvdata";

		var csv = await Http.GetStringAsync(url, ct);
		var rates = new List<ExchangeRate>();

		foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1)) {
			var cols = TextUtils.SplitCsv(line);
			if (cols.Count < 2) {
				continue;
			}

			var currency = cols.FirstOrDefault(x => x.Length == 3 && x.All(char.IsLetter) && x != "EUR");
			var numeric = cols.LastOrDefault(x => decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out _));

			if (currency is null || numeric is null) {
				continue;
			}

			if (decimal.TryParse(numeric, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate > 0) {
				rates.Add(new ExchangeRate(date, NativeCurrency, fromCurrency, rate, Code));
			}
		}
		return rates;
	}
}
