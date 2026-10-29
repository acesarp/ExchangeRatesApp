
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Globalization;
using System.Text.Json;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary> Banco de México </summary>
public sealed class BANXICOProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BANXICOProvider> _logger;
	public BANXICOProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BANXICOProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (string.IsNullOrWhiteSpace(ApiKey)) {
			throw new InvalidOperationException("Missing ProviderKeys:BANXICO.");
		}

		// Each supported currency maps to a SIE series (e.g. SF43718 = USD FIX, SF46406 = JPY), all quoted in pesos per unit.
		var seriesId = Bank.SupportedCurrencies.FirstOrDefault(s => s.Currency.CurrencyCode == quoteCurrency)?.ProviderSeriesId;

		if (string.IsNullOrWhiteSpace(seriesId)) {
			return [];
		}

		var url = $"{ApiUrl.TrimEnd('/')}/{seriesId}/datos/{fromDate:yyyy-MM-dd}/{toDate:yyyy-MM-dd}";
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Add("Bmx-Token", ApiKey);
		using var response = await Http.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();
		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		var series = doc.RootElement.GetProperty("bmx").GetProperty("series")[0];

		if (!series.TryGetProperty("datos", out var data)) {
			return [];
		}

		var rates = new List<ExchangeRateResult>();

		// "datos": [{ "fecha": "29/09/2026", "dato": "18.0710" }]; missing observations are "N/E".
		foreach (var item in data.EnumerateArray()) {
			if (!DateOnly.TryParseExact(item.GetProperty("fecha").GetString(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
				!decimal.TryParse(item.GetProperty("dato").GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
		}

		return rates;
	}
}

