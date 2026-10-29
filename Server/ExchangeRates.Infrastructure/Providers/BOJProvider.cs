
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Text.Json;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Bank of Japan
/// </summary>
public sealed class BOJProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BOJProvider> _logger;

	public BOJProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BOJProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency != "USD") {
			return [];
		}

		try {
			// FM08 = foreign exchange rates database, FXERD04 = USD/JPY spot rate at 17:00 JST (Tokyo market close).
			var uri = $"{ApiUrl}?format=json&lang=en&db=FM08&code=FXERD04&startDate={fromDate:yyyyMM}&endDate={toDate:yyyyMM}";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("RESULTSET", out var resultSet) || resultSet.ValueKind != JsonValueKind.Array || resultSet.GetArrayLength() == 0) {
				return results;
			}

			var values = resultSet[0].GetProperty("VALUES");
			var dates = values.GetProperty("SURVEY_DATES").EnumerateArray().ToList();
			var rates = values.GetProperty("VALUES").EnumerateArray().ToList();

			for (var i = 0; i < Math.Min(dates.Count, rates.Count); i++) {
				if (rates[i].ValueKind != JsonValueKind.Number || !DateOnly.TryParseExact(dates[i].GetRawText(), "yyyyMMdd", out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = rates[i].GetDecimal();

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BOJ rates.");
			return [];
		}
	}
}

