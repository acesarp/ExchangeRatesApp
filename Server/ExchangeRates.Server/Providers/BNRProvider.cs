using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banca Națională a României
/// </summary>
public sealed class BNRProvider : CentralBankProviderBase {
	private readonly ILogger<BNRProvider> _logger;

	public BNRProvider(HttpClient http, IConfiguration configuration, ILogger<BNRProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BNR";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var uri = $"{Url}/nbrfxrates.xml?date={date:yyyy-MM-dd}";
				using var response = await Http.GetAsync(uri, ct);

				if (!response.IsSuccessStatusCode) {
					continue;
				}

				var xml = await response.Content.ReadAsStringAsync(ct);
				var document = XDocument.Parse(xml);

				foreach (var rateElement in document.Descendants().Where(e => e.Name.LocalName == "Rate")) {
					var currencyAttr = rateElement.Attribute("currency")?.Value;

					if (currencyAttr is null || !currencyAttr.Equals(quoteCurrency.ToString(), StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					if (!decimal.TryParse(rateElement.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
						continue;
					}

					var multiplierAttr = rateElement.Attribute("multiplier")?.Value;
					var multiplier = multiplierAttr is not null && decimal.TryParse(multiplierAttr, NumberStyles.Any, CultureInfo.InvariantCulture, out var m) ? m : 1m;

					if (InverseProvider) {
						rate = 1m / rate;
					}
					results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate / multiplier, Code));
				}
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to fetch BNR rate for {Date}", date);
			}
		}

		return results;
	}
}
