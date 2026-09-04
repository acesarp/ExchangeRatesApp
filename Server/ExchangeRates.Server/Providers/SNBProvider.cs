using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Swiss National Bank
/// </summary>
public sealed class SNBProvider : CentralBankProviderBase {
	private readonly ILogger<SNBProvider> _logger;
	private Dictionary<ECurrencyISO, (string SeriesCode, decimal Units)>? _series;

	public SNBProvider(HttpClient http, IConfiguration configuration, ILogger<SNBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "SNB";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency == NativeCurrency) {
			return [];
		}

		var series = await GetSeriesAsync(ct);

		if (!series.TryGetValue(quoteCurrency, out var seriesInfo)) {
			_logger.LogWarning("SNB series not found for currency {Currency}", quoteCurrency);
			return [];
		}

		var url = $"{Url.TrimEnd('/')}/data/json/en" +
			$"?dimSel=D0(M0),D1({seriesInfo.SeriesCode})" +
			$"&fromDate={fromDate:yyyy-MM}" +
			$"&toDate={toDate:yyyy-MM}";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));

		if (!doc.RootElement.TryGetProperty("timeseries", out var timeSeries)) {
			return [];
		}

		var rates = new List<ExchangeRateResult>();

		foreach (var seriesElement in timeSeries.EnumerateArray()) {
			if (!seriesElement.TryGetProperty("values", out var values)) {
				continue;
			}

			foreach (var observation in values.EnumerateArray()) {
				if (!observation.TryGetProperty("date", out var dateElement) ||
					!observation.TryGetProperty("value", out var valueElement)) {
					continue;
				}

				var dateText = dateElement.GetString();

				if (!DateOnly.TryParseExact($"{dateText}-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
					!valueElement.TryGetDecimal(out var value) ||
					value <= 0) {
					continue;
				}

				// SNB returns CHF per 1/100 units of the foreign currency.
				// We store CHF -> quoteCurrency.
				var rate = seriesInfo.Units / value;

				if (InverseProvider) {
					rate = 1m / rate;
				}

				rates.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}
		}

		return rates;
	}

	private async Task<IReadOnlyDictionary<ECurrencyISO, (string SeriesCode, decimal Units)>> GetSeriesAsync(CancellationToken ct) {
		if (_series is not null) {
			return _series;
		}

		var url = $"{Url.TrimEnd('/')}/dimensions/en";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));

		_series = [];

		if (!doc.RootElement.TryGetProperty("dimensions", out var dimensions)) {
			return _series;
		}

		var currencyDimension = dimensions.EnumerateArray()
			.FirstOrDefault(x => x.TryGetProperty("id", out var id) && id.GetString() == "D1");

		if (currencyDimension.ValueKind == JsonValueKind.Undefined ||
			!currencyDimension.TryGetProperty("dimensionItems", out var dimensionItems)) {
			return _series;
		}

		ExtractSeries(dimensionItems, _series);

		return _series;
	}

	private static void ExtractSeries(JsonElement items, Dictionary<ECurrencyISO, (string SeriesCode, decimal Units)> series) {
		foreach (var item in items.EnumerateArray()) {
			if (item.TryGetProperty("dimensionItems", out var children)) {
				ExtractSeries(children, series);
				continue;
			}

			if (!item.TryGetProperty("id", out var idElement)) {
				continue;
			}

			var seriesCode = idElement.GetString();

			if (string.IsNullOrWhiteSpace(seriesCode)) {
				continue;
			}

			var match = Regex.Match(seriesCode, @"^([A-Z]{3})(\d+)$");

			if (!match.Success ||
				!Enum.TryParse<ECurrencyISO>(match.Groups[1].Value, out var currency) ||
				!decimal.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out var units)) {
				continue;
			}

			series[currency] = (seriesCode, units);
		}
	}
}