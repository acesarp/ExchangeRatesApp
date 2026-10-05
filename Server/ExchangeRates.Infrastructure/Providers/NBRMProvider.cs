
using System.Text.Json;
using System.Globalization;
using System.Xml.Linq;

using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Narodna Banka na Republika Severna Makedonija
/// </summary>
public sealed class NBRMProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<NBRMProvider> _logger;

	public NBRMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBRMProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency;
			var uri = $"{ApiUrl}?startDate={fromDate:dd.MM.yyyy}&endDate={toDate:dd.MM.yyyy}&currencyCode={currencyCode}";
			var xml = await Http.GetStringAsync(uri, ct);

			// The prolog declares utf-16 but the body is utf-8; loading from a string reader ignores the declared encoding.
			using var reader = new StringReader(xml);
			var xdoc = XDocument.Load(reader);

			var results = new List<ExchangeRateResult>();

			// The service returns every currency for the range, so filter on the ISO code (Oznaka).
			foreach (var item in xdoc.Descendants("GetExchangeRate")) {
				if (!string.Equals(item.Element("Oznaka")?.Value, currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!DateTime.TryParse(item.Element("Datum")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime)) {
					continue;
				}

				var date = DateOnly.FromDateTime(dateTime);

				if (date < fromDate || date > toDate) {
					continue;
				}

				if (!decimal.TryParse(item.Element("Sreden")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				if (!decimal.TryParse(item.Element("Nomin")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var nominal) || nominal <= 0) {
					nominal = 1m;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate / nominal, Bank.BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch NBRM rates.");
			return [];
		}
	}
}

