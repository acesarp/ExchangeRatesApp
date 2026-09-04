using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Ethiopia
/// </summary>
public sealed class NBEProvider : CentralBankProviderBase {
	private readonly ILogger<NBEProvider> _logger;
	public NBEProvider(HttpClient http, IConfiguration configuration, ILogger<NBEProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBE";
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency.ToString();
			var uri = $"{Url}?currency={currencyCode}&from_date={fromDate:yyyy-MM-dd}&to_date={toDate:yyyy-MM-dd}";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();
			var root = doc.RootElement;
			var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

			if (items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in items.EnumerateArray()) {
				if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "selling_rate");

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
			_logger.LogWarning(ex, "Failed to fetch NBE rates.");
			return [];
		}
	}
}
