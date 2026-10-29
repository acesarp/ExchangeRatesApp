
using System.Globalization;
using System.Xml.Linq;

using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Central Bank of Russia
/// </summary>
public sealed class CBRProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<CBRProvider> _logger;
	public CBRProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<CBRProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var historicalUrl = HistoricalUrl;

			if (string.IsNullOrWhiteSpace(historicalUrl)) {
				return [];
			}

			// XML_dynamic takes the CBR internal currency id (e.g. R01235 for USD), not the ISO code; look it up from the daily list.
			var dailyXml = XDocument.Parse(await GetWindows1251StringAsync(ApiUrl, ct));
			var currencyId = dailyXml.Descendants("Valute")
									.FirstOrDefault(v => string.Equals(v.Element("CharCode")?.Value, quoteCurrency, StringComparison.OrdinalIgnoreCase))?
									.Attribute("ID")?.Value;

			if (currencyId is null) {
				return [];
			}

			var uri = $"{historicalUrl}?date_req1={fromDate:dd'/'MM'/'yyyy}&date_req2={toDate:dd'/'MM'/'yyyy}&VAL_NM_RQ={currencyId}";
			var xdoc = XDocument.Parse(await GetWindows1251StringAsync(uri, ct));

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

	private async Task<string> GetWindows1251StringAsync(string url, CancellationToken ct) {
		// CBR responds in windows-1251, which .NET doesn't decode without the code pages provider.
		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
		var bytes = await Http.GetByteArrayAsync(url, ct);
		return System.Text.Encoding.GetEncoding(1251).GetString(bytes);
	}
}

