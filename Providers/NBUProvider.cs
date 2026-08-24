using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Natsionalnyi Bank Ukrainy
/// </summary>
public sealed class NBUProvider : CentralBankProviderBase {
	public NBUProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBU";
	public override string Name => "Natsionalnyi Bank Ukrainy";
	public override ECurrency NativeCurrency => ECurrency.UAH;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url =
			$"{Url}?date={fromDate:yyyyMMdd}&json";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
		var rates = new List<ExchangeRate>();

		foreach (var row in doc.RootElement.EnumerateArray()) {
			var code = row.TryGetProperty("cc", out var c) ? c.GetString() : null;
			var rate = GetDecimal(row, "rate");

			if (!string.IsNullOrWhiteSpace(code) && rate > 0) {
				rates.Add(new ExchangeRate(fromDate, code!, NativeCurrency, rate, Code));
			}
		}
		return rates;
	}
}
