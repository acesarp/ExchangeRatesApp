
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// South African Reserve Bank
/// </summary>
public sealed class SARBProvider : CentralBankProviderBase {
	private readonly ILogger<SARBProvider> _logger;

	public SARBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<SARBProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency;
			var uri = $"{Url}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&currency={currencyCode}";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();
			var root = doc.RootElement;
			var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

			if (items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in items.EnumerateArray()) {
				if (!item.TryGetProperty("Date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "Value");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.Currency.Code, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch SARB rates.");
			return [];
		}
	}
}

