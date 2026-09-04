using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central do Brasil
/// </summary>
public sealed class BCBProvider : CentralBankProviderBase {
	private readonly ILogger<BCBProvider> _logger;

	public BCBProvider(HttpClient http, IConfiguration configuration, ILogger<BCBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCB";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var rates = new List<ExchangeRateResult>();

		var url = $"{Url.TrimEnd('/')}/CotacaoMoedaPeriodo(moeda=@moeda,dataInicial=@dataInicial,dataFinalCotacao=@dataFinalCotacao)" +
			$"?@moeda='{quoteCurrency}'&@dataInicial='{fromDate:MM-dd-yyyy}'&@dataFinalCotacao='{toDate:MM-dd-yyyy}'&$format=json";

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

			if (InverseProvider) {
				rate = 1 / rate;
			}

			rates.Add(new ExchangeRateResult(DateOnly.FromDateTime(dateTime), quoteCurrency, NativeCurrency, rate, Code));
		}

		return rates;
	}
}