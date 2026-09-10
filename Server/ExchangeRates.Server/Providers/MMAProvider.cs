
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Maldives Monetary Authority
/// </summary>
public sealed class MMAProvider : CentralBankProviderBase {
	private readonly ILogger<MMAProvider> _logger;
	public MMAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<MMAProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var json = await Http.GetStringAsync(Url, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();
			var root = doc.RootElement;
			var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

			if (items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			var currencyCode = quoteCurrency;

			foreach (var item in items.EnumerateArray()) {
				var Bank.Code = item.TryGetProperty("currency", out var c) ? c.GetString() : null;

				if (!string.Equals(Bank.Code, currencyCode, StringComparison.OrdinalIgnoreCase)) {
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
				
					
				results.Add(new ExchangeRateResult(date, Bank.Currency.Code, quoteCurrency, rate, Bank.Code));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch MMA rates.");
			return [];
		}
	}
}

