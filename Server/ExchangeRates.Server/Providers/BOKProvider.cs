using ExchangeRates.Domain.Entities;

using System.Globalization;
using System.Text.Json;

namespace ExchangeRates.Server.Providers;

public sealed class BOKProvider : CentralBankProviderBase {
	public BOKProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank) : base(http, configuration, bank) { }

	private static readonly Dictionary<string, (string ItemCode, decimal Unit)> Currencies = new(StringComparer.OrdinalIgnoreCase) {
		["USD"] = ("0000001", 1m),
		["JPY"] = ("0000002", 100m),
		["EUR"] = ("0000003", 1m),
		["GBP"] = ("0000012", 1m),
		["CNY"] = ("0000053", 1m)
	};

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		if (string.IsNullOrWhiteSpace(ApiKey)) {
			throw new InvalidOperationException($"Missing CentralBanks:{Bank.Currency.CurrencyCode}:ApiKey configuration.");
		}

		var day = fromDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
		var rates = new List<ExchangeRateResult>();

		foreach (var (currency, info) in Currencies) {
			var url = $"{Url.TrimEnd('/')}/{ApiKey}/json/en/1/10/731Y001/D/{day}/{day}/{info.ItemCode}";

			using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));

			if (!doc.RootElement.TryGetProperty("StatisticSearch", out var search) ||
				!search.TryGetProperty("row", out var rows) ||
				rows.GetArrayLength() == 0) {
				continue;
			}

			var value = GetDecimal(rows[0], "DATA_VALUE");

			if (value <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(fromDate, Bank.Currency.CurrencyCode, quoteCurrency, value / info.Unit, Bank.BankCode));
		}

		return rates;
	}


}
