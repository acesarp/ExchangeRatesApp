using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

using HtmlAgilityPack;

using System.Globalization;
/// <summary>
/// Banco Central de Bolivia
/// </summary>
public sealed class BCBOProvider : CentralBankProviderBase {
	private readonly ILogger<BCBOProvider> _logger;

	public BCBOProvider(HttpClient http, IConfiguration configuration, ILogger<BCBOProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCBO";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency != ECurrencyISO.USD) {
			throw new NotSupportedException($"{Code} does not support {quoteCurrency}.");
		}

		var results = new List<ExchangeRateResult>();

		for (var year = fromDate.Year; year <= toDate.Year; year++) {
			var url = $"https://www.bcb.gob.bo/tiposDeCambioHistorico/?anio={year}";
			var html = await Http.GetStringAsync(url, ct);

			var document = new HtmlDocument();
			document.LoadHtml(html);

			var rows = document.DocumentNode.SelectNodes("//table//tr");

			if (rows == null) {
				continue;
			}

			foreach (var row in rows) {
				var cells = row.SelectNodes("./td");

				if (cells == null || cells.Count < 3) {
					continue;
				}

				var dayText = cells[0].InnerText.Trim();

				if (!int.TryParse(dayText, out var day)) {
					continue;
				}

				for (var month = 1; month <= 12; month++) {
					var sellIndex = 1 + ((month - 1) * 2);

					if (sellIndex >= cells.Count) {
						continue;
					}

					if (!DateOnly.TryParseExact($"{year}-{month:D2}-{day:D2}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
						continue;
					}

					if (date < fromDate || date > toDate) {
						continue;
					}

					var sellText = HtmlEntity.DeEntitize(cells[sellIndex].InnerText).Trim();

					if (string.IsNullOrWhiteSpace(sellText)) {
						continue;
					}

					if (!decimal.TryParse(sellText.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var bobPerUsd)) {
						continue;
					}

					if (bobPerUsd <= 0) {
						continue;
					}

					var rate = 1m / bobPerUsd;

					if (InverseProvider) {
						rate = 1m / rate;
					}
					results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
				}
			}
		}

		return results.OrderBy(x => x.Date).ToList();
	}
}