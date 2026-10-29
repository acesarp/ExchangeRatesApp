
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Text.Json;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Monetary Authority of Macao
/// </summary>
public sealed class AMCMProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<AMCMProvider> _logger;
	public AMCMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<AMCMProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{ApiUrl}?QueryType=1&Begin={fromDate:yyyyMMdd}&End={toDate:yyyyMMdd}";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
		var rates = new List<ExchangeRateResult>();

		if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) {
			return rates;
		}

		foreach (var row in data.EnumerateArray()) {
			// Rows hold every currency per day: { "date": "2026-09-30 00:00:00", "currency": "JPY", "unit": 100, "usdMeanValue": 5.1438 } (MOP per unit)
			var currencyCode = row.TryGetProperty("currency", out var c) ? c.GetString() : null;
			if (!string.Equals(currencyCode, quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			if (!row.TryGetProperty("date", out var dateProp) || !DateTime.TryParse(dateProp.GetString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dateTime)) {
				continue;
			}

			var date = DateOnly.FromDateTime(dateTime);

			if (date < fromDate || date > toDate) {
				continue;
			}

			var unit = GetDecimal(row, "unit");
			var value = GetDecimal(row, "usdMeanValue");
			if (unit <= 0 || value <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, value / unit, Bank.BankCode));
		}
		return rates;
	}
}

