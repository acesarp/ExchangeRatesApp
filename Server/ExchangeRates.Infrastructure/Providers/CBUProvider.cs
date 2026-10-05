
using ExchangeRates.Domain.Entities;

using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Central Bank of Uzbekistan
/// </summary>
public sealed class CBUProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<CBUProvider> _logger;

	public CBUProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBUProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		// The configured ApiUrl (.../json/all/) only returns today's rates; historical rates are served per currency and date
		// at .../json/{currency}/{yyyy-MM-dd}/
		var jsonUrl = ApiUrl[..(ApiUrl.IndexOf("/json", StringComparison.OrdinalIgnoreCase) + "/json".Length)];

		for (var day = fromDate; day <= toDate; day = day.AddDays(1)) {
			try {
				var json = await Http.GetStringAsync($"{jsonUrl}/{quoteCurrency}/{day:yyyy-MM-dd}/", ct);
				using var doc = JsonDocument.Parse(json);

				if (doc.RootElement.ValueKind != JsonValueKind.Array) {
					continue;
				}

				foreach (var item in doc.RootElement.EnumerateArray()) {
					var currencyCode = item.TryGetProperty("Ccy", out var c) ? c.GetString() : null;

					if (!string.Equals(currencyCode, quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					if (!item.TryGetProperty("Date", out var dateProp) || !DateOnly.TryParseExact(dateProp.GetString(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
						continue;
					}

					// For non-business days the API returns the last published rate with its original date.
					if (date < fromDate || date > toDate || results.Any(r => r.Date == date)) {
						continue;
					}

					var rate = GetDecimal(item, "Rate");
					var nominal = GetDecimal(item, "Nominal");
					nominal = nominal <= 0 ? 1m : nominal;

					if (rate <= 0) {
						continue;
					}

					results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate / nominal, Bank.BankCode));
				}
			}
			catch (Exception ex) when (ex is not OperationCanceledException) {
				_logger.LogWarning(ex, "Failed to fetch CBU rate for {Date}", day);
			}
		}

		return results;
	}
}

