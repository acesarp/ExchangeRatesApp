
using ExchangeRates.Domain.Entities;

using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Maldives Monetary Authority
/// </summary>
public sealed class MMAProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<MMAProvider> _logger;
	public MMAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<MMAProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		// The feed only publishes the MVR/USD reference rate: [{ "Date": "05 October 2026", "Rate": "15.42" }, ...]
		if (!string.Equals(quoteCurrency, "USD", StringComparison.OrdinalIgnoreCase)) {
			return [];
		}

		try {
			var json = await Http.GetStringAsync(ApiUrl, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (doc.RootElement.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in doc.RootElement.EnumerateArray()) {
				if (!item.TryGetProperty("Date", out var dateProp) ||
					!DateOnly.TryParseExact(dateProp.GetString(), "dd MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "Rate");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch MMA rates.");
			return [];
		}
	}
}

