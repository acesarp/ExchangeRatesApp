using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bangko Sentral ng Pilipinas
/// </summary>
public sealed class BSPProvider : CentralBankProviderBase {
	private readonly ILogger<BSPProvider> _logger;

	public BSPProvider(HttpClient http, IConfiguration configuration, ILogger<BSPProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BSP";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			using var request = new HttpRequestMessage(HttpMethod.Get, Url);
			request.Headers.TryAddWithoutValidation("Accept", "application/json;odata=verbose");

			using var response = await Http.SendAsync(request, ct);

			if (!response.IsSuccessStatusCode) {
				return [];
			}

			var json = await response.Content.ReadAsStringAsync(ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("d", out var d) || !d.TryGetProperty("results", out var items) || items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			var currencyCode = quoteCurrency.ToString();

			foreach (var item in items.EnumerateArray()) {
				var code = item.TryGetProperty("Currency", out var c) ? c.GetString() : null;

				if (!string.Equals(code, currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!item.TryGetProperty("Date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "Rate");

				if (rate <= 0) {
					continue;
				}

				if (InverseProvider) {
					rate = 1m / rate;
				}
				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BSP rates.");
			return [];
		}
	}
}
