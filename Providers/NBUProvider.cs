using ExchangeRates.Server;
using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Extensions;
using ExchangeRates.Server.Providers;

using System.Globalization;
using System.Text.Json;

/// <summary>
/// Natsionalnyi Bank Ukrainy
/// </summary>
public sealed class NBUProvider : CentralBankProviderBase {
	public NBUProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBU";
	public override string Name => "Natsionalnyi Bank Ukrainy";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.UAH;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrencyISO currency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url =
			$"{Url}?start={fromDate:yyyyMMdd}" +
			$"&end={toDate:yyyyMMdd}" +
			$"&valcode={currency}" +
			"&sort=exchangedate" +
			"&order=asc" +
			"&json";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
		var rates = new List<ExchangeRate>();

		foreach (var row in doc.RootElement.EnumerateArray()) {
			var code = row.TryGetProperty("cc", out var c) ? c.GetString() : null;
			var dateText = row.TryGetProperty("exchangedate", out var d) ? d.GetString() : null;

			if (string.IsNullOrWhiteSpace(code) ||
				string.IsNullOrWhiteSpace(dateText) ||
				!DateOnly.TryParseExact(dateText, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			var rate = GetDecimal(row, "rate_per_unit");

			if (rate <= 0) {
				rate = GetDecimal(row, "rate");
			}

			if (rate > 0) {
				rates.Add(new ExchangeRate(
					date,
					code.ToEnum<ECurrencyISO>(),
					NativeCurrency,
					rate,
					Code));
			}
		}

		return rates;
	}
}