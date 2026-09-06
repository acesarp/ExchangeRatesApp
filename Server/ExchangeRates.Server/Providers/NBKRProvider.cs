using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of the Kyrgyz Republic
/// </summary>
public sealed class NBKRProvider : CentralBankProviderBase {
	private readonly ILogger<NBKRProvider> _logger;
	public NBKRProvider(HttpClient http, IConfiguration configuration, ILogger<NBKRProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBKR";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var xml = await Http.GetStringAsync(Url, ct);
			var xdoc = XDocument.Parse(xml);

			var results = new List<ExchangeRateResult>();
			var dateStr = xdoc.Root?.Attribute("Date")?.Value;

			if (dateStr is null || !DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, out var date)) {
				return results;
			}

			if (date < fromDate || date > toDate) {
				return results;
			}

			var currencyCode = quoteCurrency.ToString();
			var currencyNode = xdoc.Descendants("Currency").FirstOrDefault(c => string.Equals(c.Attribute("ISOCode")?.Value, currencyCode, StringComparison.OrdinalIgnoreCase));

			if (currencyNode is null) {
				return results;
			}

			var valueStr = currencyNode.Element("Value")?.Value;
			var nominalStr = currencyNode.Element("Nominal")?.Value;

			if (valueStr is null || !decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) || value <= 0) {
				return results;
			}

			var nominal = 1m;

			if (nominalStr is not null && decimal.TryParse(nominalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedNominal) && parsedNominal > 0) {
				nominal = parsedNominal;
			}

			results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, value / nominal, Code));
			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch NBKR rates.");
			return [];
		}
	}
}
