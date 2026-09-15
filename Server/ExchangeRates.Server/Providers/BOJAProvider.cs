
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Jamaica
/// </summary>
public sealed class BOJAProvider : CentralBankProviderBase {
	private readonly ILogger<BOJAProvider> _logger;

	public BOJAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BOJAProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			using var content = new FormUrlEncodedContent(new Dictionary<string, string> {
				["action"] = "get_exchange_rates",
				["from"] = fromDate.ToString("yyyy-MM-dd"),
				["to"] = toDate.ToString("yyyy-MM-dd"),
				["currency"] = quoteCurrency
			});

			using var response = await Http.PostAsync(Url, content, ct);

			if (!response.IsSuccessStatusCode) {
				return [];
			}

			var json = await response.Content.ReadAsStringAsync(ct);
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

				var rate = GetDecimal(item, "sellingRate");

				if (rate <= 0) {
					continue;
				}

				
					
				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BOJA rates.");
			return [];
		}
	}
}

