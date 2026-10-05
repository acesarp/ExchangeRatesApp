
using ExchangeRates.Domain.Models;
using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Türkiye Cumhuriyet Merkez Bankası
/// </summary>
public sealed class TCMBProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<TCMBProvider> _logger;
	public TCMBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<TCMBProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var rates = new List<ExchangeRateResult>();

		// One XML file per business day: {base}/{yyyyMM}/{ddMMyyyy}.xml. Weekends and holidays return 404.
		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) {
				continue;
			}

			var url = $"{ApiUrl.TrimEnd('/')}/{date:yyyyMM}/{date:ddMMyyyy}.xml";
			using var response = await Http.GetAsync(url, ct);

			if (!response.IsSuccessStatusCode) {
				continue;
			}

			var doc = System.Xml.Linq.XDocument.Parse(await response.Content.ReadAsStringAsync(ct));
			var node = doc.Descendants("Currency").FirstOrDefault(c => string.Equals(c.Attribute("CurrencyCode")?.Value, quoteCurrency, StringComparison.OrdinalIgnoreCase));

			if (node is null) {
				continue;
			}

			var unitText = node.Element("Unit")?.Value;
			var forexBuyingText = node.Element("ForexBuying")?.Value;
			var forexSellingText = node.Element("ForexSelling")?.Value;

			if (!decimal.TryParse(unitText, NumberStyles.Any, CultureInfo.InvariantCulture, out var unit) || unit <= 0) {
				continue;
			}

			decimal.TryParse(forexBuyingText, NumberStyles.Any, CultureInfo.InvariantCulture, out var buy);
			decimal.TryParse(forexSellingText, NumberStyles.Any, CultureInfo.InvariantCulture, out var sell);
			var mid = buy > 0 && sell > 0 ? (buy + sell) / 2m : Math.Max(buy, sell);

			if (mid > 0) {
				rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, mid / unit, Bank.BankCode));
			}
		}

		return rates;
	}
}

