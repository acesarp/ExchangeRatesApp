
using System.Globalization;
using System.Xml.Linq;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Seðlabanki Íslands
/// </summary>
public sealed class SBIProvider : CentralBankProviderBase {
	private readonly ILogger<SBIProvider> _logger;

	public SBIProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<SBIProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency;
			var uri = $"{Url}?currBase={currencyCode}&dateFrom={fromDate:yyyy-MM-dd}&dateTo={toDate:yyyy-MM-dd}";
			var xml = await Http.GetStringAsync(uri, ct);
			var xdoc = XDocument.Parse(xml);

			var results = new List<ExchangeRateResult>();

			foreach (var entry in xdoc.Descendants("Entry")) {
				var dateStr = entry.Element("Date")?.Value;

				if (dateStr is null || !DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rateStr = entry.Element("Value")?.Value;

				if (rateStr is null || !decimal.TryParse(rateStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, Bank.Currency.Code, quoteCurrency, rate, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch SBI rates.");
			return [];
		}
	}
}

