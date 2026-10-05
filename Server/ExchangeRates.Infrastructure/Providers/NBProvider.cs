
using ExchangeRates.Domain.Entities;

using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Norges Bank
/// </summary>
public sealed class NBProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<NBProvider> _logger;

	public NBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency;
			// Series key is FREQ.BASE_CUR.QUOTE_CUR.TENOR, e.g. B.USD.NOK.SP (NOK per USD, business-daily spot).
			var dataflowUrl = ApiUrl[..(ApiUrl.IndexOf("/EXR", StringComparison.OrdinalIgnoreCase) + "/EXR".Length)];
			var uri = $"{dataflowUrl}/B.{currencyCode}.NOK.SP?format=sdmx-json&startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}";
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

					// Observation values are serialized as strings, e.g. "0": ["9.2703"]
					var rateElement = obs.Value[0];
					decimal rate;
					if (rateElement.ValueKind == JsonValueKind.Number) {
						rate = rateElement.GetDecimal();
					}
					else if (rateElement.ValueKind != JsonValueKind.String || !decimal.TryParse(rateElement.GetString(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out rate)) {
						continue;
					}

					if (rate <= 0) {
						continue;
					}

					results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
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

