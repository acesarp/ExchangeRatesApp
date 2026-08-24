using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banca d'Italia
/// </summary>
public sealed class BDIProvider : CentralBankProviderBase {
	public BDIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BDI";
	public override string Name => "Banca d'Italia";
	public override ECurrency NativeCurrency => ECurrency.EUR;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?referenceDate={date:yyyy-MM-dd}&currencyIsoCode=EUR&lang=en";

		var csv = await Http.GetStringAsync(url, ct);
		var rates = new List<ExchangeRate>();

		foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1)) {
			var cols = TextUtils.SplitCsv(line);
			if (cols.Count < 3) {
				continue;
			}

			var code = cols.FirstOrDefault(x => x.Length == 3 && x.All(char.IsLetter));
			var rateText = cols.FirstOrDefault(x => decimal.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out _));

			if (code is null || rateText is null) {
				continue;
			}

			if (decimal.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) && rate > 0) {
				rates.Add(new ExchangeRate(fromDate, NativeCurrency, code, rate, Code));
			}
		}

		return rates;
	}

}
