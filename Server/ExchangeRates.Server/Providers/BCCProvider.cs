
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Cuba
/// </summary>
public sealed class BCCProvider : CentralBankProviderBase {
	private readonly ILogger<BCCProvider> _logger;
	public BCCProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCCProvider> logger) : base(http, configuration, bank) {
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

			foreach (var item in items.EnumerateArray()) {
				if (!item.TryGetProperty("moneda", out var currencyProp) || !currencyProp.GetString()!.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				var rate = GetDecimal(item, "venta");

				if (rate <= 0) {
					continue;
				}

				var date = item.TryGetProperty("fecha", out var dateProp) && DateOnly.TryParse(dateProp.GetString(), out var d) ? d : DateOnly.FromDateTime(DateTime.UtcNow);

				if (date < fromDate || date > toDate) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.Currency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BCC rates.");
			return [];
		}
	}
}

