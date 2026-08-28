using ExchangeRates.Blazor.Client.Models;

using System.Net.Http.Json;

namespace ExchangeRates.Blazor.Client.Services;

public class CurrencyService {
	private readonly HttpClient _http;

	public CurrencyService(HttpClient http) {
		_http = http;
	}

	public async Task<List<string>> GetCurrenciesAsync(CancellationToken ct = default) {
		var response = await _http.GetFromJsonAsync<List<string>>("api/available-currencies", ct);

		return response;
	}

	public async Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(DateOnly fromDate, DateOnly toDate, string baseCurrency, string quoteCurrency, CancellationToken ct = default) {
		var response = await _http.GetFromJsonAsync<List<ExchangeRateResult>>($"api/exchange-rates?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&baseCurrency={baseCurrency}&quoteCurrency={quoteCurrency}", ct) ?? [];
		return response;
	}
}