using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Thailand
/// </summary>
public sealed class BOTProvider : CentralBankProviderBase {
	private readonly ILogger<BOTProvider> _logger;

	public BOTProvider(HttpClient http, IConfiguration configuration, ILogger<BOTProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOT";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var apiKey = ApiKey;

		if (string.IsNullOrWhiteSpace(apiKey)) {
			_logger.LogWarning("Missing CentralBanks:BOT:ApiKey.");
			return [];
		}

		try {
			var uri = $"{Url.TrimEnd('/')}?start_period={fromDate:yyyy-MM-dd}&end_period={toDate:yyyy-MM-dd}&currency={quoteCurrency}";
			using var request = new HttpRequestMessage(HttpMethod.Get, uri);
			request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", apiKey);

			using var response = await Http.SendAsync(request, ct);

			if (!response.IsSuccessStatusCode) {
				return [];
			}

			var json = await response.Content.ReadAsStringAsync(ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("result", out var result) || !result.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in data.EnumerateArray()) {
				if (!item.TryGetProperty("period", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "mid_rate");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BOT rates.");
			return [];
		}
	}
}
