
using System.Globalization;
using System.Xml.Linq;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Lietuvos Bankas
/// </summary>
public sealed class LBProvider : CentralBankProviderBase {
	private readonly ILogger<LBProvider> _logger;
	public LBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<LBProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency;
			var uri = $"{Url}?tp=EU&ccy={currencyCode}&dtFrom={fromDate:yyyy-MM-dd}&dtTo={toDate:yyyy-MM-dd}";
			var xml = await Http.GetStringAsync(uri, ct);
			var xdoc = XDocument.Parse(xml);

			var results = new List<ExchangeRateResult>();

			foreach (var element in xdoc.Descendants("FxRate")) {
				var dateStr = element.Element("Dt")?.Value;

				if (dateStr is null || !DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var amountStr = element.Elements("CcyAmt").FirstOrDefault(e => e.Element("Ccy")?.Value != "EUR")?.Element("Amt")?.Value;

				if (amountStr is null || !decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				
					
				results.Add(new ExchangeRateResult(date, Bank.Currency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch LB rates.");
			return [];
		}
	}
}

