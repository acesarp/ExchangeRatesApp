
using ExchangeRates.Domain.Entities;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bangko Sentral ng Pilipinas
/// </summary>
public sealed class BSPProvider : CentralBankProviderBase {
	private readonly ILogger<BSPProvider> _logger;

	public BSPProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BSPProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			using var request = new HttpRequestMessage(HttpMethod.Get, Url);
			request.Headers.TryAddWithoutValidation("Accept", "application/json;odata=verbose");

			using var response = await Http.SendAsync(request, ct);

			if (!response.IsSuccessStatusCode) {
				return [];
			}

			var json = await response.Content.ReadAsStringAsync(ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("d", out var d) || !d.TryGetProperty("results", out var items) || items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			var currencyCode = quoteCurrency;

			foreach (var item in items.EnumerateArray()) {
				var bankCurrencyCode = item.TryGetProperty("NativeCurrency", out var c) ? c.GetString() : null;

				if (!string.Equals(bankCurrencyCode, currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!item.TryGetProperty("Date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "Rate");

				if (rate <= 0) {
					continue;
				}



				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BSP rates.");
			return [];
		}
	}
}

