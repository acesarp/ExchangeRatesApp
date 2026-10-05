
using ExchangeRates.Domain.Entities;

using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Narodowy Bank Polski
/// </summary>
public sealed class NBPProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<NBPProvider> _logger;

	public NBPProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBPProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		quoteCurrency = quoteCurrency.ToUpperInvariant();
		var rates = new List<ExchangeRateResult>();

		// The NBP API limits a single range query to 93 days.
		for (var chunkStart = fromDate; chunkStart <= toDate; chunkStart = chunkStart.AddDays(93)) {
			var chunkEnd = chunkStart.AddDays(92) < toDate ? chunkStart.AddDays(92) : toDate;
			var url = $"{ApiUrl.TrimEnd('/')}/rates/A/{quoteCurrency}/{chunkStart:yyyy-MM-dd}/{chunkEnd:yyyy-MM-dd}/?format=json";
			using var response = await Http.GetAsync(url, ct);

			// 404 means no rates were published in this range (weekends, holidays).
			if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
				continue;
			}

			response.EnsureSuccessStatusCode();
			using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

			foreach (var row in doc.RootElement.GetProperty("rates").EnumerateArray()) {
				if (!DateOnly.TryParse(row.GetProperty("effectiveDate").GetString(), out var date)) {
					continue;
				}

				var rate = GetDecimal(row, "mid");

				if (rate > 0) {
					rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				}
			}
		}
		return rates;
	}
}

