
using ExchangeRates.Domain.Entities;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Czech National Bank
/// </summary>
public sealed class CNBProvider : CentralBankProviderBase {
	private readonly ILogger<CNBProvider> _logger;

	public CNBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CNBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var results = new List<ExchangeRateResult>();
			var currencyCode = quoteCurrency;

			for (var year = fromDate.Year; year <= toDate.Year; year++) {
				var uri = $"{Url}?year={year}";
				var json = await Http.GetStringAsync(uri, ct);
				using var doc = JsonDocument.Parse(json);

				if (!doc.RootElement.TryGetProperty("rates", out var rates) || rates.ValueKind != JsonValueKind.Array) {
					continue;
				}

				foreach (var item in rates.EnumerateArray()) {
					var bankCode = item.TryGetProperty("currencyCode", out var c) ? c.GetString() : null;

					if (!string.Equals(bankCode, currencyCode, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					if (!item.TryGetProperty("validFor", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
						continue;
					}

					if (date < fromDate || date > toDate) {
						continue;
					}

					var rate = GetDecimal(item, "rate");
					var amount = GetDecimal(item, "amount");
					amount = amount <= 0 ? 1m : amount;

					if (rate <= 0) {
						continue;
					}



					results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate / amount, bankCode));
				}
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CNB rates.");
			return [];
		}
	}
}

