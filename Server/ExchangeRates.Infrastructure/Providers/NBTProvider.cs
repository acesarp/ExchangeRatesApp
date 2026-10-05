
using ExchangeRates.Domain.Entities;

using System.Globalization;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// National Bank of Tajikistan
/// </summary>
public sealed class NBTProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<NBTProvider> _logger;

	public NBTProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBTProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var results = new List<ExchangeRateResult>();
			var currencyCode = quoteCurrency;

			for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
				var uri = $"{ApiUrl}?date={date:yyyy-MM-dd}&export=xmlout";
				var xml = await Http.GetStringAsync(uri, ct);
				var xdoc = XDocument.Parse(xml);

				var currencyNode = xdoc.Descendants("Valute").FirstOrDefault(c => string.Equals(c.Element("CharCode")?.Value, currencyCode, StringComparison.OrdinalIgnoreCase));

				if (currencyNode is null) {
					continue;
				}

				var valueStr = currencyNode.Element("Value")?.Value;
				var nominalStr = currencyNode.Element("Nominal")?.Value;

				if (valueStr is null || !decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) || value <= 0) {
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
			_logger.LogWarning(ex, "Failed to fetch NBT rates.");
			return [];
		}
	}
}

