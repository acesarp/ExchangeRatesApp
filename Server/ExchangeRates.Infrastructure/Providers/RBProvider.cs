
using ExchangeRates.Domain.Entities;

using System.Text.Json;
using System.Globalization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Sveriges Riksbank
/// </summary>
public sealed class RBProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<RBProvider> _logger;

	public RBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<RBProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			// SWEA series are named SEK{currency}PMI (SEK per unit, daily fixing). The configured ApiUrl points at
			// .../Observations/ByGroup/{id}; a single series is served at .../Observations/{seriesId}/{from}/{to}.
			var observationsUrl = ApiUrl[..(ApiUrl.IndexOf("/Observations", StringComparison.OrdinalIgnoreCase) + "/Observations".Length)];
			var uri = $"{observationsUrl}/SEK{quoteCurrency.ToUpperInvariant()}PMI/{fromDate:yyyy-MM-dd}/{toDate:yyyy-MM-dd}";
			using var response = await Http.GetAsync(uri, ct);

			// 204/404: no observations in range, or no series for this currency.
			if (!response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent) {
				return [];
			}

			using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

			var results = new List<ExchangeRateResult>();

			if (doc.RootElement.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in doc.RootElement.EnumerateArray()) {
				if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "value");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch RB rates.");
			return [];
		}
	}
}

