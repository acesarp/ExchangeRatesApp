
using System.Text.Json;

using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// National Bank of Ethiopia
/// </summary>
public sealed class NBEProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<NBEProvider> _logger;
	public NBEProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBEProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		// The API filters by a single date only and returns every currency for it:
		// { "data": [{ "date": "2026-10-01", "weighted_average": "155.12", "currency": { "code": "USD" } }] }
		for (var day = fromDate; day <= toDate; day = day.AddDays(1)) {
			try {
				var json = await Http.GetStringAsync($"{ApiUrl}?date={day:yyyy-MM-dd}", ct);
				using var doc = JsonDocument.Parse(json);

				if (!doc.RootElement.TryGetProperty("data", out var items) || items.ValueKind != JsonValueKind.Array) {
					continue;
				}

				foreach (var item in items.EnumerateArray()) {
					var currencyCode = item.TryGetProperty("currency", out var currency) && currency.TryGetProperty("code", out var code) ? code.GetString() : null;

					if (!string.Equals(currencyCode, quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, out var date)) {
						continue;
					}

					var rate = GetDecimal(item, "weighted_average");

					if (rate <= 0) {
						continue;
					}

					results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				}
			}
			catch (Exception ex) when (ex is not OperationCanceledException) {
				_logger.LogWarning(ex, "Failed to fetch NBE rates for {Date}", day);
			}
		}

		return results;
	}
}

