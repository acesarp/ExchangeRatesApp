
using ExchangeRates.Domain.Entities;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Nigeria
/// </summary>
public sealed class CBNProvider : CentralBankProviderBase {
	private readonly ILogger<CBNProvider> _logger;

	public CBNProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBNProvider> logger) : base(http, bank, configuration) {
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
			var today = DateOnly.FromDateTime(DateTime.UtcNow);

			if (today < fromDate || today > toDate) {
				return results;
			}

			foreach (var item in items.EnumerateArray()) {
				var BankCode = item.TryGetProperty("currency", out var c) ? c.GetString() : null;

				if (!string.Equals(BankCode, currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				var rate = GetDecimal(item, "sellingRate");

				if (rate <= 0) {
					continue;
				}



				results.Add(new ExchangeRateResult(today, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBN rates.");
			return [];
		}
	}
}

