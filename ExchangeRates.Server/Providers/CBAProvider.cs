using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Armenia
/// </summary>
public sealed class CBAProvider : CentralBankProviderBase {
	public CBAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBA";
	public override string Name => "Central Bank of Armenia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.AMD;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		if (quoteCurrency == NativeCurrency) {
			return [];
		}

		var isoCodes = string.Join(",", SupportedCurrencies.Where(c => c != NativeCurrency)
																															.Select(c => c.ToString()));

		var soap = $"""
			<?xml version="1.0" encoding="utf-8"?>
			<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
						   xmlns:xsd="http://www.w3.org/2001/XMLSchema"
						   xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
				<soap:Body>
					<ExchangeRatesByDateRangeByISO xmlns="http://www.cba.am/">
						<ISOCodes>{isoCodes}</ISOCodes>
						<DateFrom>{fromDate:yyyy-MM-dd}T00:00:00</DateFrom>
						<DateTo>{toDate:yyyy-MM-dd}T00:00:00</DateTo>
					</ExchangeRatesByDateRangeByISO>
				</soap:Body>
			</soap:Envelope>
			""";

		using var request = new HttpRequestMessage(HttpMethod.Post, Url);
		request.Headers.Add("SOAPAction", "\"http://www.cba.am/ExchangeRatesByDateRangeByISO\"");
		request.Content = new StringContent(soap, Encoding.UTF8, "text/xml");

		using var response = await Http.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();

		var xml = await response.Content.ReadAsStringAsync(ct);
		var document = XDocument.Parse(xml);
		var rates = new List<ExchangeRate>();

		foreach (var row in document.Descendants().Where(x => x.Name.LocalName == "ExchangeRate")) {
			var iso = row.Elements().FirstOrDefault(x => x.Name.LocalName == "ISO")?.Value;
			var amountText = row.Elements().FirstOrDefault(x => x.Name.LocalName == "Amount")?.Value;
			var rateText = row.Elements().FirstOrDefault(x => x.Name.LocalName == "Rate")?.Value;

			var dateText =
				row.Elements().FirstOrDefault(x => x.Name.LocalName == "Date")?.Value ??
				row.Elements().FirstOrDefault(x => x.Name.LocalName == "CurrentDate")?.Value;

			if (string.IsNullOrWhiteSpace(iso) ||
				!decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) ||
				!decimal.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
				!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
				amount <= 0 ||
				rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRate(DateOnly.FromDateTime(date), Enum.Parse<ECurrencyISO>(iso), NativeCurrency, rate / amount, Code));
		}

		return rates;
	}
}
