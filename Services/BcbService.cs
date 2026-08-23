namespace ExchangeRates.Server.Services;

using ExchangeRates.Server.Configuration;
using ExchangeRates.Server.Models;

using Microsoft.Extensions.Options;

using System.Net.Http.Json;

public class BcbService {
	private readonly HttpClient _httpClient;
	private readonly BcbApiOptions _options;

	public BcbService(HttpClient httpClient, IOptions<BcbApiOptions> options) {
		_httpClient = httpClient;
		_options = options.Value;
	}

	public async Task<List<BcbQuote>> GetQuoteAsync(DateTime date, ICurrency currency = ICurrency.USD) {
		var formattedDate = date.ToString("MM-dd-yyyy");

		var url =
			$"{_options.BaseUrl}{_options.EndPoint}" +
			$"CotacaoMoedaDia(moeda=@moeda,dataCotacao=@dataCotacao)" +
			$"?@moeda='{currency}'" +
			$"&@dataCotacao='{formattedDate}'" +
			$"&$format=json";

		try {
			var response = await _httpClient.GetFromJsonAsync<BcbResponse>(url);
			return response?.Value;
		}
		catch (Exception ex) {
			Console.WriteLine(ex);
			throw;
		}
	}
}