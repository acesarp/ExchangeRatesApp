
using ExchangeRates.Domain.Entities;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Nacional de Angola
/// </summary>
public sealed class BNAProvider : CentralBankProviderBase {
	private readonly ILogger<BNAProvider> _logger;

	public BNAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BNAProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var json = await Http.GetStringAsync(ApiUrl, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();
			var today = DateOnly.FromDateTime(DateTime.UtcNow);

			if (today < fromDate || today > toDate) {
				return results;
			}

			var root = doc.RootElement;
			var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

			if (items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in items.EnumerateArray()) {
				if (!item.TryGetProperty("currencySymbol", out var currencyProp) || !currencyProp.GetString()!.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				var rate = GetDecimal(item, "sellValue");

				if (rate <= 0) {
					continue;
				}



				results.Add(new ExchangeRateResult(today, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BNA rates.");
			return [];
		}
	}
}

