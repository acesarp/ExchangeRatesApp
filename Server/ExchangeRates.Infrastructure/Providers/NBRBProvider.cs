
using ExchangeRates.Domain.Entities;

using System.Globalization;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Natsyyanalny Bank Respubliki Belarus
/// </summary>
public sealed class NBRBProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<NBRBProvider> _logger;

	public NBRBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBRBProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var rates = new List<ExchangeRateResult>();
		var baseUrl = ApiUrl.TrimEnd('/');

		// Resolve the NBRB internal currency id and scale (parammode=2 = lookup by ISO code), then fetch the whole range at once.
		using var currencyDoc = JsonDocument.Parse(await Http.GetStringAsync($"{baseUrl}/{quoteCurrency}?parammode=2", ct));
		var currencyId = currencyDoc.RootElement.GetProperty("Cur_ID").GetInt32();
		var scale = GetDecimal(currencyDoc.RootElement, "Cur_Scale");
		scale = scale <= 0 ? 1m : scale;

		var url = $"{baseUrl}/dynamics/{currencyId}?startdate={fromDate:yyyy-MM-dd}&enddate={toDate:yyyy-MM-dd}";
		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));

		foreach (var item in doc.RootElement.EnumerateArray()) {
			if (!item.TryGetProperty("Date", out var dateProp) || !DateTime.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime)) {
				continue;
			}

			var rate = GetDecimal(item, "Cur_OfficialRate");

			if (rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(DateOnly.FromDateTime(dateTime), Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate / scale, Bank.BankCode));
		}

		return rates;
	}
}

