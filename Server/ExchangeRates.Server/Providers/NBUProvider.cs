using ExchangeRates.Domain.Enums;
using ExchangeRates.Server;
using ExchangeRates.Server.Extensions;
using ExchangeRates.Server.Providers;

using System.Globalization;
using System.Text.Json;

/// <summary>
/// Natsionalnyi Bank Ukrainy
/// </summary>
public sealed class NBUProvider : CentralBankProviderBase {
	private readonly ILogger<NBUProvider> _logger;

	public NBUProvider(HttpClient http, IConfiguration configuration, ILogger<NBUProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBU";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO currency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url =
			$"{Url}?start={fromDate:yyyyMMdd}" +
			$"&end={toDate:yyyyMMdd}" +
			$"&valcode={currency}" +
			"&sort=exchangedate" +
			"&order=asc" +
			"&json";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
		var rates = new List<ExchangeRateResult>();

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
				if (InverseProvider) {
					rate = 1m / rate;
				}
				rates.Add(new ExchangeRateResult(date, NativeCurrency, code.ToECurrency(), rate, Code));
			}
		}

		return rates;
	}
}