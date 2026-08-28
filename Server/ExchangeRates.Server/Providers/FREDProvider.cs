using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Federal Reserve Bank of St. Louis
/// </summary>
public sealed class FREDProvider : CentralBankProviderBase {
	private readonly ILogger<FREDProvider> _logger;
	public FREDProvider(HttpClient http, IConfiguration configuration, ILogger<FREDProvider> logger) : base(http, configuration) {
		_logger = logger;
		Series = Configuration.GetSection("CentralBanks:FRED:SupportedCurrencies").Get<List<FREDProviderCurrencyConfiguration>>();
	}

	public override string Code => "FRED";
	public override string Name => "Federal Reserve Bank of St. Louis"; //Federal Reserve USA
	public override ECurrencyISO NativeCurrency => ECurrencyISO.USD;

	private readonly List<FREDProviderCurrencyConfiguration> Series;

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var series = Series.FirstOrDefault(s => s.Currency == quoteCurrency);
		if (series == default) {
			_logger.LogWarning("FRED series not found for quote currency: {QuoteCurrency}", quoteCurrency);
			return [];
		}

		var apiKey = Configuration[$"ProviderKeys:{Code}"];

		if (string.IsNullOrWhiteSpace(apiKey)) {
			throw new InvalidOperationException("FRED API key is not configured.");
		}

		var url = $"{Url}" +
			$"?series_id={series.SeriesId}" +
			$"&api_key={Uri.EscapeDataString(apiKey)}" +
			"&file_type=json" +
			$"&observation_start={fromDate:yyyy-MM-dd}" +
			$"&observation_end={toDate:yyyy-MM-dd}";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));

		if (!doc.RootElement.TryGetProperty("observations", out var observations)) {
			return [];
		}

		var rates = new List<ExchangeRateResult>();

		foreach (var observation in observations.EnumerateArray()) {
			var dateText = observation.GetProperty("date").GetString();
			var valueText = observation.GetProperty("value").GetString();

			if (!DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
				!decimal.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
				rate <= 0) {
				continue;
			}

			if (series.Invert) {
				rate = 1m / rate;
			}

			rates.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
		}

		return rates;
	}


}
public class FREDProviderCurrencyConfiguration {
	public ECurrencyISO Currency { get; set; }
	public string SeriesId { get; set; } = string.Empty;
	public bool Invert { get; set; }
}