
using ExchangeRates.Domain.Entities;

using System.Globalization;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// National Bank of Kazakhstan
/// </summary>
public sealed class NBKProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<NBKProvider> _logger;
	public NBKProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBKProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		// The RSS feed returns all currencies for a single date (fdate); range parameters are ignored.
		for (var day = fromDate; day <= toDate; day = day.AddDays(1)) {
			try {
				var xml = await Http.GetStringAsync($"{ApiUrl}?fdate={day:dd.MM.yyyy}", ct);
				var xdoc = XDocument.Parse(xml);

				var dateStr = xdoc.Root?.Element("date")?.Value;

				if (dateStr is null || !DateOnly.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date != day) {
					continue;
				}

				// <item><title>USD</title><description>537.51</description><quant>1</quant></item>
				var item = xdoc.Descendants("item").FirstOrDefault(i => string.Equals(i.Element("title")?.Value, quoteCurrency, StringComparison.OrdinalIgnoreCase));

				if (item is null || !decimal.TryParse(item.Element("description")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				if (!decimal.TryParse(item.Element("quant")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0) {
					quantity = 1m;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate / quantity, Bank.BankCode));
			}
			catch (Exception ex) when (ex is not OperationCanceledException) {
				_logger.LogWarning(ex, "Failed to fetch NBK rate for {Date}", day);
			}
		}

		return results;
	}
}

