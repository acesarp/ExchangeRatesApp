
using ExchangeRates.Domain.Entities;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Türkiye Cumhuriyet Merkez Bankası
/// </summary>
public sealed class TCMBProvider : CentralBankProviderBase {
	private readonly ILogger<TCMBProvider> _logger;
	public TCMBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<TCMBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url.TrimEnd('/')}/{fromDate:yyyyMM}/{toDate:ddMMyyyy}.xml";
		var xml = await Http.GetStringAsync(url, ct);

		var doc = System.Xml.Linq.XDocument.Parse(xml);
		var rates = new List<ExchangeRateResult>();

		foreach (var node in doc.Descendants("NativeCurrency")) {
			var currencyCode = node.Attribute("CurrencyCode")?.Value;
			var unitText = node.Element("Unit")?.Value;
			var forexBuyingText = node.Element("ForexBuying")?.Value;
			var forexSellingText = node.Element("ForexSelling")?.Value;

			if (string.IsNullOrWhiteSpace(currencyCode) ||
				!decimal.TryParse(unitText, NumberStyles.Any, CultureInfo.InvariantCulture, out var unit) ||
				unit <= 0) {
				continue;
			}

			decimal.TryParse(forexBuyingText, NumberStyles.Any, CultureInfo.InvariantCulture, out var buy);
			decimal.TryParse(forexSellingText, NumberStyles.Any, CultureInfo.InvariantCulture, out var sell);

			var mid = buy > 0 && sell > 0 ? (buy + sell) / 2m : Math.Max(buy, sell);

			if (mid > 0) {

				rates.Add(new ExchangeRateResult(fromDate, Bank.NativeCurrency.CurrencyCode, quoteCurrency, mid / unit, BankCode));
			}
		}
		return rates;
	}
}

