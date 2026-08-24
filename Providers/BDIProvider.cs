using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banca d'Italia
/// </summary>
public sealed class BDIProvider : CentralBankProviderBase {
	public BDIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BDI";
	public override string Name => "Banca d'Italia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.EUR;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrencyISO fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var rates = new List<ExchangeRate>();

		for (var requestedDate = fromDate; requestedDate <= toDate; requestedDate = requestedDate.AddDays(1)) {
			var url = $"{Url}?referenceDate={requestedDate:yyyy-MM-dd}&currencyIsoCode={fromCurrency}&lang=en";
			using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));

			if (!doc.RootElement.TryGetProperty("rates", out var observations)) {
				continue;
			}

			foreach (var observation in observations.EnumerateArray()) {
				var code = observation.TryGetProperty("isoCode", out var isoCode) ? isoCode.GetString() : null;
				var dateText = observation.TryGetProperty("referenceDate", out var referenceDate) ? referenceDate.GetString() : null;
				var rate = GetDecimal(observation, "avgRate");

				if (!Enum.TryParse<ECurrencyISO>(code, out var currency) ||
					!DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
					rate <= 0) {
					continue;
				}

				rates.Add(new ExchangeRate(date, NativeCurrency, currency, rate, Code));
			}
		}

		return rates;
	}

}
