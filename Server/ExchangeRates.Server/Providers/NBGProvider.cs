
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Georgia
/// </summary>
public sealed class NBGProvider : CentralBankProviderBase {
	private readonly ILogger<NBGProvider> _logger;
	public NBGProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBGProvider> logger) : base(http, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var results = new List<ExchangeRateResult>();
			var currencyCode = quoteCurrency;

			for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
				var uri = $"{Url}{currencyCode}?date={date:yyyy-MM-dd}";
				var json = await Http.GetStringAsync(uri, ct);
				using var doc = JsonDocument.Parse(json);

				if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) {
					continue;
				}

				var entry = doc.RootElement[0];

				if (!entry.TryGetProperty("currencies", out var currencies) || currencies.ValueKind != JsonValueKind.Array || currencies.GetArrayLength() == 0) {
					continue;
				}

				var currencyEntry = currencies[0];
				var rate = GetDecimal(currencyEntry, "rate");
				var quantity = GetDecimal(currencyEntry, "quantity");
				quantity = quantity <= 0 ? 1m : quantity;

				if (rate <= 0) {
					continue;
				}

				
					
				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate / quantity, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch NBG rates.");
			return [];
		}
	}
}

