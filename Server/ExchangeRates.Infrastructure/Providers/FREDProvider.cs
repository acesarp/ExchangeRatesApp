
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Globalization;
using System.Text.Json;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Federal Reserve Bank of St. Louis
/// </summary>
public sealed class FREDProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.ForeignPerNative;

	private readonly ILogger<FREDProvider> _logger;
	public FREDProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<FREDProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var seriesId = Bank.SupportedCurrencies.FirstOrDefault(s => s.Currency.CurrencyCode == quoteCurrency)?.ProviderSeriesId;
		if (string.IsNullOrWhiteSpace(seriesId)) {
			_logger.LogWarning("FRED series not found for quote currency: {QuoteCurrency}", quoteCurrency);
			return Array.Empty<ExchangeRateResult>();
		}

		var apiKey = Configuration[$"ProviderKeys:{Bank.BankCode}"];

		if (string.IsNullOrWhiteSpace(apiKey)) {
			throw new InvalidOperationException("FRED API key is not configured.");
		}

		var url = $"{ApiUrl}" +
			$"?series_id={seriesId}" +
			$"&api_key={Uri.EscapeDataString(apiKey)}" +
			"&file_type=json" +
			$"&observation_start={fromDate:yyyy-MM-dd}" +
			$"&observation_end={toDate:yyyy-MM-dd}";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));

		if (!doc.RootElement.TryGetProperty("observations", out var observations)) {
			return Array.Empty<ExchangeRateResult>();
		}

		var rates = new List<ExchangeRateResult>();

		foreach (var observation in observations.EnumerateArray()) {
			var dateText = observation.GetProperty("date").GetString();
			var valueText = observation.GetProperty("value").GetString();

			if (!DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
				!decimal.TryParse(valueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
				rate <= 0) {
				continue;
			}

			// DEXUS{XX} series are quoted as USD per foreign unit; flip them to foreign units per USD like the DEX{XX}US series.
			if (seriesId.StartsWith("DEXUS", StringComparison.OrdinalIgnoreCase)) {
				rate = 1m / rate;
			}

			rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
		}

		return rates;
	}
}
