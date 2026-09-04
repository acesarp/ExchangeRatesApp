using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Japan
/// </summary>
public sealed class BOJProvider : CentralBankProviderBase {
	private readonly ILogger<BOJProvider> _logger;

	public BOJProvider(HttpClient http, IConfiguration configuration, ILogger<BOJProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOJ";
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency != ECurrencyISO.USD) {
			return [];
		}

		try {
			var uri = $"{Url}?code=FM01&from={fromDate:yyyy-MM}&to={toDate:yyyy-MM}";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in data.EnumerateArray()) {
				if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "value");

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
			_logger.LogWarning(ex, "Failed to fetch BOJ rates.");
			return [];
		}
	}
}
