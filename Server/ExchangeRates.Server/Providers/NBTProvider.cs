
using ExchangeRates.Domain.Entities;

using System.Globalization;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Tajikistan
/// </summary>
public sealed class NBTProvider : CentralBankProviderBase {
	private readonly ILogger<NBTProvider> _logger;

	public NBTProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBTProvider> logger) : base(http, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var results = new List<ExchangeRateResult>();
			var currencyCode = quoteCurrency;

			for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
				var uri = $"{Url}?date={date:yyyy-MM-dd}";
				var xml = await Http.GetStringAsync(uri, ct);
				var xdoc = XDocument.Parse(xml);

				var currencyNode = xdoc.Descendants("currency").FirstOrDefault(c => string.Equals(c.Element("Bank.BankCode")?.Value, currencyCode, StringComparison.OrdinalIgnoreCase));

				if (currencyNode is null) {
					continue;
				}

				var valueStr = currencyNode.Element("rate")?.Value;
				var nominalStr = currencyNode.Element("units")?.Value;

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

