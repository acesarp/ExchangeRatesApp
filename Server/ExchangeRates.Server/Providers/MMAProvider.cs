using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Maldives Monetary Authority
/// </summary>
public sealed class MMAProvider : CentralBankProviderBase {
	private readonly ILogger<MMAProvider> _logger;
	public MMAProvider(HttpClient http, IConfiguration configuration, ILogger<MMAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "MMA";
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var json = await Http.GetStringAsync(Url, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();
			var root = doc.RootElement;
			var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

			if (items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			var currencyCode = quoteCurrency.ToString();

			foreach (var item in items.EnumerateArray()) {
				var code = item.TryGetProperty("currency", out var c) ? c.GetString() : null;

				if (!string.Equals(code, currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "rate");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch MMA rates.");
			return [];
		}
	}
}
