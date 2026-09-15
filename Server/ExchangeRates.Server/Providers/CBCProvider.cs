
using System.Globalization;
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of the Republic of China (Taiwan)
/// </summary>
public sealed class CBCProvider : CentralBankProviderBase {
	private readonly ILogger<CBCProvider> _logger;
	public CBCProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBCProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency != "USD") {
			return [];
		}

		try {
			var json = await Http.GetStringAsync(Url, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in data.EnumerateArray()) {
				if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 2) {
					continue;
				}

				var elements = item.EnumerateArray().ToArray();
				var dateStr = elements[0].GetString();

				if (dateStr is null || !DateOnly.TryParseExact(dateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				if (elements[1].ValueKind != JsonValueKind.Number || elements[1].GetDecimal() <= 0) {
					continue;
				}

				var rate = elements[1].GetDecimal();

				
					
				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBC rates.");
			return [];
		}
	}
}

