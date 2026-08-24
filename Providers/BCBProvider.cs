using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central do Brasil
/// </summary>
public sealed class BCBProvider : CentralBankProviderBase {
	public BCBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCB";
	public override string Name => "Banco Central do Brasil";
	public override ECurrency NativeCurrency => ECurrency.BRL;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var currencies = new[] { "AUD", "CAD", "CHF", "DKK", "EUR", "GBP", "JPY", "NOK", "SEK", "USD" };
		var rates = new List<ExchangeRate>();

		var url = $"{Url.TrimEnd('/')}/CotacaoMoedaPeriodo(moeda=@moeda,dataInicial=@dataInicial,dataFinalCotacao=@dataFinalCotacao)" +
				$"?@moeda='{fromCurrency}'&@dataInicial='{fromDate:MM-dd-yyyy}'&@dataFinalCotacao='{toDate:MM-dd-yyyy}'&$format=json";


		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
		if (!doc.RootElement.TryGetProperty("value", out var values)) {
			return [];
		}

		foreach (var row in values.EnumerateArray()) {
			var buy = GetDecimal(row, "cotacaoCompra");
			var sell = GetDecimal(row, "cotacaoVenda");
			var rate = buy > 0 && sell > 0 ? (buy + sell) / 2m : Math.Max(buy, sell);
			if (rate <= 0) {
				continue;
			}
			var dateText = row.GetProperty("dataHoraCotacao").GetString();

			if (!DateTime.TryParse(dateText, out var dateTime)) {
				continue;
			}
			rates.Add(new ExchangeRate(DateOnly.FromDateTime(dateTime), fromCurrency, NativeCurrency, rate, Code));

		}
		return rates;
	}
}
