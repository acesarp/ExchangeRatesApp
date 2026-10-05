
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Text.Json;
using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Bank Negara Malaysia
/// </summary>
public sealed class BNMProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BNMProvider> _logger;

	public BNMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BNMProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		// The API serves one calendar month per request: /exchange-rate/{currency}/year/{yyyy}/month/{m}
		for (var month = new DateOnly(fromDate.Year, fromDate.Month, 1); month <= toDate; month = month.AddMonths(1)) {
			try {
				var uri = $"{ApiUrl}/{quoteCurrency}/year/{month.Year}/month/{month.Month}?session=1200&quote=rm";
				using var request = new HttpRequestMessage(HttpMethod.Get, uri);
				request.Headers.Add("Accept", "application/vnd.BNM.API.v1+json");
				using var response = await Http.SendAsync(request, ct);

				if (!response.IsSuccessStatusCode) {
					continue;
				}

				var json = await response.Content.ReadAsStringAsync(ct);
				using var doc = JsonDocument.Parse(json);

				if (!doc.RootElement.TryGetProperty("data", out var data) || !data.TryGetProperty("rate", out var rateList) || rateList.ValueKind != JsonValueKind.Array) {
					continue;
				}

				var unit = GetDecimal(data, "unit");
				unit = unit <= 0 ? 1m : unit;

				foreach (var item in rateList.EnumerateArray()) {
					if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, out var date)) {
						continue;
					}

					if (date < fromDate || date > toDate) {
						continue;
					}

					var rate = GetDecimal(item, "middle_rate");

					if (rate <= 0) {
						continue;
					}

					results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate / unit, Bank.BankCode));
				}
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to fetch BNM rates for {Month:yyyy-MM}", month);
			}
		}

		return results;
	}
}

