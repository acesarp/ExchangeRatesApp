
using System.Globalization;
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Hrvatska Narodna Banka
/// </summary>
public sealed class HNBProvider : CentralBankProviderBase {
	private readonly ILogger<HNBProvider> _logger;

	public HNBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<HNBProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency;
			var uri = $"{Url}?currency={currencyCode}&date_from={fromDate:yyyy-MM-dd}&date_to={toDate:yyyy-MM-dd}";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (doc.RootElement.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in doc.RootElement.EnumerateArray()) {
				if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rateStr = item.TryGetProperty("middle_rate", out var r) ? r.GetString() : null;

				if (rateStr is null || !decimal.TryParse(rateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				
					
				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch HNB rates.");
			return [];
		}
	}
}

