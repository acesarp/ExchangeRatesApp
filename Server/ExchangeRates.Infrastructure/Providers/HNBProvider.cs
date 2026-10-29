
using System.Globalization;
using System.Text.Json;

using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Hrvatska Narodna Banka
/// </summary>
public sealed class HNBProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.ForeignPerNative;

	private readonly ILogger<HNBProvider> _logger;

	public HNBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<HNBProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		// The v3 API ignores range parameters, so request one application date (datum-primjene) at a time.
		for (var day = fromDate; day <= toDate; day = day.AddDays(1)) {
			try {
				var uri = $"{ApiUrl}?valuta={quoteCurrency}&datum-primjene={day:yyyy-MM-dd}";
				var json = await Http.GetStringAsync(uri, ct);
				using var doc = JsonDocument.Parse(json);

				if (doc.RootElement.ValueKind != JsonValueKind.Array) {
					continue;
				}

				// [{ "datum_primjene": "2026-09-29", "valuta": "USD", "srednji_tecaj": "1,137800", ... }] (1 EUR = x currency)
				foreach (var item in doc.RootElement.EnumerateArray()) {
					if (!string.Equals(item.TryGetProperty("valuta", out var c) ? c.GetString() : null, quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					if (!item.TryGetProperty("datum_primjene", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, out var date)) {
						continue;
					}

					if (date < fromDate || date > toDate) {
						continue;
					}

					var rateStr = item.TryGetProperty("srednji_tecaj", out var r) ? r.GetString() : null;

					if (rateStr is null || !decimal.TryParse(rateStr.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
						continue;
					}

					results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				}
			}
			catch (Exception ex) when (ex is not OperationCanceledException) {
				_logger.LogWarning(ex, "Failed to fetch HNB rate for {Date}", day);
			}
		}

		return results;
	}
}

