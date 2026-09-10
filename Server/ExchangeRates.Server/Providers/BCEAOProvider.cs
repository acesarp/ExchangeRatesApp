
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque Centrale des Etats de l'Afrique de l'Ouest
/// </summary>
public sealed class BCEAOProvider : CentralBankProviderBase {
	private readonly ILogger<BCEAOProvider> _logger;

	public BCEAOProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCEAOProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var uri = $"{Url}?date={date:yyyy-MM-dd}";
				var json = await Http.GetStringAsync(uri, ct);
				using var doc = JsonDocument.Parse(json);

				var root = doc.RootElement;
				var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

				if (items.ValueKind != JsonValueKind.Array) {
					continue;
				}

				foreach (var item in items.EnumerateArray()) {
					if (!item.TryGetProperty("devise", out var currencyProp) || !currencyProp.GetString()!.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					var rate = GetDecimal(item, "cours");

					if (rate <= 0) {
						continue;
					}

					results.Add(new ExchangeRateResult(date, Bank.Currency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				}
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to fetch BCEAO rate for {Date}", date);
			}
		}

		return results;
	}
}

