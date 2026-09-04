using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Norges Bank
/// </summary>
public sealed class NBProvider : CentralBankProviderBase {
	private readonly ILogger<NBProvider> _logger;

	public NBProvider(HttpClient http, IConfiguration configuration, ILogger<NBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NB";
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency.ToString();
			var uri = Url.Replace("NOK", currencyCode) + $"?format=sdmx-json&startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}";
			using var request = new HttpRequestMessage(HttpMethod.Get, uri);
			request.Headers.TryAddWithoutValidation("Accept", "application/vnd.sdmx.data+json;version=1.0.0");

			using var response = await Http.SendAsync(request, ct);

			if (!response.IsSuccessStatusCode) {
				return [];
			}

			var json = await response.Content.ReadAsStringAsync(ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("data", out var data)
				|| !data.TryGetProperty("structure", out var structure)
				|| !structure.TryGetProperty("dimensions", out var dimensions)
				|| !dimensions.TryGetProperty("observation", out var obsDim) || obsDim.ValueKind != JsonValueKind.Array || obsDim.GetArrayLength() == 0
				|| !obsDim[0].TryGetProperty("values", out var dateValues) || dateValues.ValueKind != JsonValueKind.Array) {
				return results;
			}

			var dates = dateValues.EnumerateArray()
				.Select(v => v.TryGetProperty("id", out var id) ? id.GetString() : null)
				.ToArray();

			if (!data.TryGetProperty("dataSets", out var dataSets) || dataSets.ValueKind != JsonValueKind.Array || dataSets.GetArrayLength() == 0) {
				return results;
			}

			var dataSet = dataSets[0];

			if (!dataSet.TryGetProperty("series", out var series)) {
				return results;
			}

			foreach (var seriesEntry in series.EnumerateObject()) {
				if (!seriesEntry.Value.TryGetProperty("observations", out var observations)) {
					continue;
				}

				foreach (var obs in observations.EnumerateObject()) {
					if (!int.TryParse(obs.Name, out var index) || index >= dates.Length) {
						continue;
					}

					var dateStr = dates[index];

					if (dateStr is null || !DateOnly.TryParse(dateStr, out var date) || date < fromDate || date > toDate) {
						continue;
					}

					if (obs.Value.ValueKind != JsonValueKind.Array || obs.Value.GetArrayLength() == 0) {
						continue;
					}

					var rateElement = obs.Value[0];
					decimal rate = default;
					if (rateElement.ValueKind != JsonValueKind.Number || rateElement.GetDecimal() <= 0) {
						continue;
					}
					if (InverseProvider) {
						rate = 1m / rateElement.GetDecimal();
					}
					else {
						rate = rateElement.GetDecimal();
					}

					results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
				}
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch NB rates.");
			return [];
		}
	}
}
