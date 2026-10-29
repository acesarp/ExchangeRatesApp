
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Text.Json;
using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Bank of Mongolia
/// </summary>
public sealed class BOMProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BOMProvider> _logger;

	public BOMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BOMProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			// The endpoint is a POST that returns the full daily history regardless of the requested range:
			// { "data": [{ "RATE_DATE": "2026-10-05", "USD": "3,595.55", ... }] }
			using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl) {
				Content = new StringContent($"{{\"startDate\":\"{fromDate:yyyy-MM-dd}\",\"endDate\":\"{toDate:yyyy-MM-dd}\"}}", System.Text.Encoding.UTF8, "application/json")
			};
			request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
			using var response = await Http.SendAsync(request, ct);
			response.EnsureSuccessStatusCode();
			using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("data", out var items) || items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in items.EnumerateArray()) {
				if (!item.TryGetProperty("RATE_DATE", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				if (!item.TryGetProperty(quoteCurrency.ToUpperInvariant(), out var rateProp) ||
					!decimal.TryParse(rateProp.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BOM rates.");
			return [];
		}
	}
}

