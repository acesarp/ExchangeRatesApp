using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Türkiye Cumhuriyet Merkez Bankası
/// </summary>
public sealed class TCMBProvider : CentralBankProviderBase {
	public TCMBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "TCMB";
	public override string Name => "Türkiye Cumhuriyet Merkez Bankası";
	public override ECurrency NativeCurrency => ECurrency.TRY;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url.TrimEnd('/')}/{fromDate:yyyyMM}/{toDate:ddMMyyyy}.xml";
		var xml = await Http.GetStringAsync(url, ct);

		var doc = System.Xml.Linq.XDocument.Parse(xml);
		var rates = new List<ExchangeRate>();

		foreach (var node in doc.Descendants("Currency")) {
			var code = node.Attribute("CurrencyCode")?.Value;
			var unitText = node.Element("Unit")?.Value;
			var forexBuyingText = node.Element("ForexBuying")?.Value;
			var forexSellingText = node.Element("ForexSelling")?.Value;

			if (string.IsNullOrWhiteSpace(code) ||
				!decimal.TryParse(unitText, NumberStyles.Any, CultureInfo.InvariantCulture, out var unit) ||
				unit <= 0) {
				continue;
			}

			decimal.TryParse(forexBuyingText, NumberStyles.Any, CultureInfo.InvariantCulture, out var buy);
			decimal.TryParse(forexSellingText, NumberStyles.Any, CultureInfo.InvariantCulture, out var sell);

			var mid = buy > 0 && sell > 0 ? (buy + sell) / 2m : Math.Max(buy, sell);
			if (mid > 0) {
				rates.Add(new ExchangeRate(fromDate, code, NativeCurrency, mid / unit, Code));
			}
		}
		return rates;
	}
}
