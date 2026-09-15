
namespace ExchangeRates.Server.Providers;

using ExcelDataReader;

using ExchangeRates.Domain.Entities;

using HtmlAgilityPack;

using System.Globalization;
/// <summary>
/// Banco Central de Bolivia
/// </summary>
public sealed class BCBOProvider : CentralBankProviderBase {
	private readonly ILogger<BCBOProvider> _logger;

	public BCBOProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCBOProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (fromDate.Year == DateTime.UtcNow.Year && toDate.Year == DateTime.UtcNow.Year) {
			return await FetchCurrentYearAsync(quoteCurrency, fromDate, toDate, ct);
		}
		else {
			var results = new List<ExchangeRateResult>();
			for (var year = fromDate.Year; year <= toDate.Year; year++) {
				ct.ThrowIfCancellationRequested();
				results.AddRange(await FetchHistoryAsync(year, ct));
			}
			return results;
		}
	}

	/// <summary>
	/// Fetches the exchange rates from the Banco Central de Bolivia (BCBO) provider for the specified quote currency and date range.
	/// </summary>
	/// <param name="quoteCurrency"></param>
	/// <param name="fromDate"></param>
	/// <param name="toDate"></param>
	/// <param name="ct"></param>
	/// <returns>A read-only list of exchange rate results.</returns>
	private async Task<IReadOnlyList<ExchangeRateResult>> FetchCurrentYearAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			ct.ThrowIfCancellationRequested();

			try {
				var url = $"{Url}/librerias/indicadores/otras/otras_imprimir2XLS.php?qdd={date.Day}&qmm={date.Month}&qaa={date.Year}";
				using var response = await Http.GetAsync(url, ct);

				if (!response.IsSuccessStatusCode) {
					_logger.LogWarning("BCBO returned HTTP {StatusCode} for {date}", response.StatusCode, date);
					continue;
				}

				await using var stream = await response.Content.ReadAsStreamAsync(ct);
				using var reader = ExcelReaderFactory.CreateReader(stream);

				var rate = ExtractRate(reader, quoteCurrency);
				if (rate.HasValue) {
					results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate.Value, Bank.BankCode));
				}
			}
			catch (OperationCanceledException ex) when (ct.IsCancellationRequested) {
				_logger.LogInformation(ex, "Operation canceled while retrieving BCBO exchange rate for {NativeCurrency} on {Date}", quoteCurrency, date);
				throw;
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Unable to retrieve BCBO exchange rate for {NativeCurrency} on {Date}", quoteCurrency, date);
			}
		}
		return results;
	}


	private async Task<IReadOnlyList<ExchangeRateResult>> FetchHistoryAsync(int year, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		//Historico
		var url = $"{Url}/tiposDeCambioHistorico/?anio={year}";

		var html = await Http.GetStringAsync(url, ct);

		var document = new HtmlDocument();
		document.LoadHtml(html);

		var rows = document.DocumentNode.SelectNodes("//table//tr");

		if (rows == null) {
			return results;
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



				results.Add(new ExchangeRateResult(date, Bank.BankCode, "USD", rate, Bank.BankCode));
			}
		}
		return results.OrderBy(x => x.Date).ToList();
	}

	private static decimal? ExtractRate(IExcelDataReader reader, string quoteCurrency) {
		var requestedCode = quoteCurrency;

		while (reader.Read()) {
			var countryOrConcept = GetString(reader, 0);
			var BankCode = GetString(reader, 2);

			if (!string.Equals(BankCode, requestedCode, StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			/*
			 * USD appears in the separate official exchange-rate section:
			 *
			 * ESTADOS UNIDOS | DOLAR | USD | 12.58
			 *
			 * Other currencies appear under COTIZACION DE MONEDAS:
			 *
			 * BRASIL | REAL | BRL | 2.45272 | 5.12900
			 *
			 * Column 3 means:
			 *     Bs per unit of foreign currency
			 *
			 * Example:
			 *     1 BRL = 2.45272 BOB
			 *
			 * Our provider has BOB as the base currency, therefore:
			 *     1 BOB = 1 / 2.45272 BRL
			 */

			if (!TryGetDecimal(reader.GetValue(3), out var bobPerForeignCurrency) || bobPerForeignCurrency <= 0) {
				continue;
			}

			return 1m / bobPerForeignCurrency;
		}

		return null;
	}

	private static string GetString(IExcelDataReader reader, int column) {
		if (column >= reader.FieldCount) {
			return string.Empty;
		}

		return reader.GetValue(column)?.ToString()?.Trim() ?? string.Empty;
	}

	private static bool TryGetDecimal(object? value, out decimal result) {
		switch (value) {
			case decimal decimalValue:
				result = decimalValue;
				return true;

			case double doubleValue:
				result = Convert.ToDecimal(doubleValue);
				return true;

			case float floatValue:
				result = Convert.ToDecimal(floatValue);
				return true;

			case int intValue:
				result = intValue;
				return true;

			case long longValue:
				result = longValue;
				return true;

			default:
				return decimal.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
		}
	}

}
