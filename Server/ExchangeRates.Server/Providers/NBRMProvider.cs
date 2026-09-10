
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Narodna Banka na Republika Severna Makedonija
/// </summary>
public sealed class NBRMProvider : CentralBankProviderBase {
	private readonly ILogger<NBRMProvider> _logger;

	public NBRMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBRMProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency;
			var uri = $"{Url}?startDate={fromDate:yyyy-MM-dd}&endDate={toDate:yyyy-MM-dd}&currencyCode={currencyCode}";
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

				var rate = GetDecimal(item, "middleRate");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.Currency.Code, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch NBRM rates.");
			return [];
		}
	}
}

