using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Kazakhstan
/// </summary>
public sealed class NBKProvider : CentralBankProviderBase {
	private readonly ILogger<NBKProvider> _logger;
	public NBKProvider(HttpClient http, IConfiguration configuration, ILogger<NBKProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBK";
	public override string Name => "National Bank of Kazakhstan";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.KZT;
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency.ToString();
			var uri = $"{Url}?fdate={fromDate:dd.MM.yyyy}&t_date={toDate:dd.MM.yyyy}&ccode={currencyCode}";
			var xml = await Http.GetStringAsync(uri, ct);
			var xdoc = XDocument.Parse(xml);

			var results = new List<ExchangeRateResult>();

			foreach (var item in xdoc.Descendants("item")) {
				var dateStr = item.Element("title")?.Value;

				if (dateStr is null || !DateOnly.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var description = item.Element("description")?.Value;

				if (description is null || !decimal.TryParse(description, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch NBK rates.");
			return [];
		}
	}
}
