
using System.Globalization;
using System.Xml.Linq;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Russia
/// </summary>
public sealed class CBRProvider : CentralBankProviderBase {
	private readonly ILogger<CBRProvider> _logger;
	public CBRProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBRProvider> logger) : base(http, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var historicalUrl = HistoricalUrl;

			if (string.IsNullOrWhiteSpace(historicalUrl)) {
				return [];
			}

			var currencyCode = quoteCurrency;
			var uri = $"{historicalUrl}?date_req1={fromDate:dd/MM/yyyy}&date_req2={toDate:dd/MM/yyyy}&VAL_NM_RQ={currencyCode}";
			var xml = await Http.GetStringAsync(uri, ct);
			var xdoc = XDocument.Parse(xml);

			var results = new List<ExchangeRateResult>();

			foreach (var record in xdoc.Descendants("Record")) {
				var dateStr = record.Attribute("Date")?.Value;

				if (dateStr is null || !DateOnly.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var valueStr = record.Element("Value")?.Value;
				var nominalStr = record.Element("Nominal")?.Value;

				if (valueStr is null || !decimal.TryParse(valueStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) || value <= 0) {
					continue;
				}

				var nominal = 1m;

				if (nominalStr is not null && decimal.TryParse(nominalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedNominal) && parsedNominal > 0) {
					nominal = parsedNominal;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, value / nominal, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBR rates.");
			return [];
		}
	}
}

