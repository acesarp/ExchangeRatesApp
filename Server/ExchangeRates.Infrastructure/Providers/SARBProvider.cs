
using ExchangeRates.Domain.Entities;

using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// South African Reserve Bank
/// </summary>
public sealed class SARBProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<SARBProvider> _logger;

	public SARBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<SARBProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		// SARB publishes daily rand rates (10:30 weighted average) only for these currencies.
		if (!SeriesCodes.TryGetValue(quoteCurrency, out var seriesCode)) {
			return [];
		}

		try {
			var uri = $"{ApiUrl.TrimEnd('/')}/{seriesCode}/{fromDate:yyyy-MM-dd}/{toDate:yyyy-MM-dd}";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (doc.RootElement.ValueKind != JsonValueKind.Array) {
				return results;
			}

			// [{ "Period": "2026-10-02T00:00:00", "Timeseries": "Rand per US Dollar", "Value": 16.6495 }, ...]
			foreach (var item in doc.RootElement.EnumerateArray()) {
				if (!item.TryGetProperty("Period", out var dateProp) || !DateTime.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime)) {
					continue;
				}

				var date = DateOnly.FromDateTime(dateTime);

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "Value");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch SARB rates.");
			return [];
		}
	}

	private static readonly Dictionary<string, string> SeriesCodes = new(StringComparer.OrdinalIgnoreCase) {
		["USD"] = "EXCX135D",
		["GBP"] = "EXCZ001D",
		["EUR"] = "EXCZ002D",
		["JPY"] = "EXCZ120D"
	};
}

