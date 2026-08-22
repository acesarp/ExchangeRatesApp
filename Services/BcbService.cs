namespace ExchangeRates.Server.Services;

using ExchangeRates.Server.Configuration;
using ExchangeRates.Server.Models;

using Microsoft.Extensions.Options;
public class BcbService {
	private readonly HttpClient _httpClient;
	private readonly BcbApiOptions _options;

	public BcbService(HttpClient httpClient, IOptions<BcbApiOptions> options) {
		_httpClient = httpClient;
		_options = options.Value;
	}

	public async Task<List<BcbQuote?>> GetQuoteAsync(DateTime date, ICurrency currency = ICurrency.USD) {
		var formattedDate = date.ToString("MM-dd-yyyy");

		var url =
			$"{_options.BaseUrl}{_options.EndPoint}" +
			$"CotacaoMoedaDia(moeda=@moeda,dataCotacao=@dataCotacao)" +
			$"?@moeda='{currency}'" +
			$"&@dataCotacao='{formattedDate}'" +
			$"&$format=json";

		try {
			var response = await _httpClient.GetStringAsync(url);
			var d = DateTime.Parse("2026-07-20 10:02:17.71519");
			return null;
		}
		catch (Exception ex) {
			Console.WriteLine(ex);
		}
		return null;

	}
}

