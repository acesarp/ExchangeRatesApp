
using ExchangeRates.Domain.Entities;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Egypt
/// </summary>
public sealed class CBEProvider : CentralBankProviderBase {
	private readonly ILogger<CBEProvider> _logger;

	public CBEProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBEProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var uri = $"{ApiUrl}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();
			var root = doc.RootElement;
			var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

			if (items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			var currencyCode = quoteCurrency;

			foreach (var item in items.EnumerateArray()) {
				var BankCode = item.TryGetProperty("currencyCode", out var c) ? c.GetString() : null;

				if (!string.Equals(BankCode, currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "sellPrice");

				if (rate <= 0) {
					continue;
				}



				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBE rates.");
			return [];
		}
	}
}

