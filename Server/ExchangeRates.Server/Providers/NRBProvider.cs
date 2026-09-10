
using ExchangeRates.Domain.Entities;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Nepal Rastra Bank
/// </summary>
public sealed class NRBProvider : CentralBankProviderBase {
	private readonly ILogger<NRBProvider> _logger;
	public NRBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NRBProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var uri = $"{Url}?page=1&from={fromDate:yyyy-MM-dd}&to={toDate:yyyy-MM-dd}&per_page=100";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("data", out var dataObj) || !dataObj.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Array) {
				return results;
			}

			var currencyCode = quoteCurrency;

			foreach (var item in payload.EnumerateArray()) {
				if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				if (!item.TryGetProperty("rates", out var rates) || rates.ValueKind != JsonValueKind.Array) {
					continue;
				}

				foreach (var rateItem in rates.EnumerateArray()) {
					var iso3 = rateItem.TryGetProperty("currency", out var c) && c.TryGetProperty("iso3", out var iso) ? iso.GetString() : null;

					if (!string.Equals(iso3, currencyCode, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					var unit = GetDecimal(rateItem, "currency") == 0 && rateItem.TryGetProperty("currency", out var currencyObj) && currencyObj.TryGetProperty("unit", out var unitProp) && unitProp.ValueKind == JsonValueKind.Number ? unitProp.GetDecimal() : 1m;
					var sell = GetDecimal(rateItem, "sell");

					if (sell <= 0) {
						continue;
					}

					results.Add(new ExchangeRateResult(date, Bank.Currency.CurrencyCode, quoteCurrency, sell / unit, Bank.BankCode));
				}
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch NRB rates.");
			return [];
		}
	}
}

