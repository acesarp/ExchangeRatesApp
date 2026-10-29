
using HtmlAgilityPack;

using System.Globalization;
using System.Net;

using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
using System.Xml.Linq;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// National Bank of Moldova
/// </summary>
public sealed class NBMProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<NBMProvider> _logger;
	public NBMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBMProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		// The official rates are published as XML per date: ?get_xml=1&date=dd.MM.yyyy
		// <ValCurs Date="01.10.2026"><Valute><CharCode>USD</CharCode><Nominal>1</Nominal><Value>17.43</Value></Valute></ValCurs>
		for (var day = fromDate; day <= toDate; day = day.AddDays(1)) {
			try {
				var xml = await Http.GetStringAsync($"{ApiUrl}?get_xml=1&date={day:dd.MM.yyyy}", ct);
				var xdoc = System.Xml.Linq.XDocument.Parse(xml);

				if (!DateOnly.TryParseExact(xdoc.Root?.Attribute("Date")?.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date != day) {
					continue;
				}

				var valute = xdoc.Descendants("Valute").FirstOrDefault(v => string.Equals(v.Element("CharCode")?.Value, quoteCurrency, StringComparison.OrdinalIgnoreCase));

				if (valute is null || !decimal.TryParse(valute.Element("Value")?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				if (!decimal.TryParse(valute.Element("Nominal")?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var nominal) || nominal <= 0) {
					nominal = 1m;
				}

				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate / nominal, Bank.BankCode));
			}
			catch (Exception ex) when (ex is not OperationCanceledException) {
				_logger.LogWarning(ex, "Failed to fetch NBM rates for {Date}", day);
			}
		}

		return results;
	}
}

