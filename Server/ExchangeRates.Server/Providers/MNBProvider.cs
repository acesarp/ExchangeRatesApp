
using ExchangeRates.Domain.Entities;

using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Magyar Nemzeti Bank
/// </summary>
public sealed class MNBProvider : CentralBankProviderBase {
	private readonly ILogger<MNBProvider> _logger;
	public MNBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<MNBProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency;
			var envelope = $"""
				<?xml version="1.0" encoding="utf-8"?>
				<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
				<soap:Body>
				<GetExchangeRatesXML xmlns="http://www.mnb.hu/webservices/">
				<startDate>{fromDate:yyyy-MM-dd}</startDate>
				<endDate>{toDate:yyyy-MM-dd}</endDate>
				<currencyNames>{currencyCode}</currencyNames>
				</GetExchangeRatesXML>
				</soap:Body>
				</soap:Envelope>
				""";

			using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
			content.Headers.Remove("Content-Type");
			content.Headers.TryAddWithoutValidation("Content-Type", "text/xml; charset=utf-8");
			using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl) { Content = content };
			request.Headers.TryAddWithoutValidation("SOAPAction", "http://www.mnb.hu/webservices/GetExchangeRatesXML");

			using var response = await Http.SendAsync(request, ct);

			if (!response.IsSuccessStatusCode) {
				return [];
			}

			var soapXml = await response.Content.ReadAsStringAsync(ct);
			var soapDoc = XDocument.Parse(soapXml);
			var resultText = soapDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "GetExchangeRatesXMLResult")?.Value;

			if (string.IsNullOrWhiteSpace(resultText)) {
				return [];
			}

			var innerDoc = XDocument.Parse(resultText);
			var results = new List<ExchangeRateResult>();

			foreach (var dayNode in innerDoc.Descendants("Day")) {
				var dateStr = dayNode.Attribute("date")?.Value;

				if (dateStr is null || !DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rateNode = dayNode.Elements("Rate").FirstOrDefault(r => string.Equals(r.Attribute("curr")?.Value, currencyCode, StringComparison.OrdinalIgnoreCase));

				if (rateNode is null) {
					continue;
				}

				var unit = decimal.TryParse(rateNode.Attribute("unit")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var u) ? u : 1m;

				if (!decimal.TryParse(rateNode.Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}



				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate / unit, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch MNB rates.");
			return [];
		}
	}
}

