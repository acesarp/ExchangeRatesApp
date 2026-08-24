using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Canada
/// </summary>
public sealed class BOCProvider : CentralBankProviderBase {
	public BOCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOC";
	public override string Name => "Bank of Canada";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.CAD;

	/// <inheritdoc/>
	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrencyISO fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?start_date={fromDate:yyyy-MM-dd}&end_date={toDate:yyyy-MM-dd}";
		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));

		if (!doc.RootElement.TryGetProperty("observations", out var observations) || observations.GetArrayLength() == 0) {
			return [];
		}

		var rates = new List<ExchangeRate>();

		foreach (var observation in observations.EnumerateArray()) {
			if (!observation.TryGetProperty("d", out var dateValue) ||
				!DateOnly.TryParse(dateValue.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
				!observation.TryGetProperty("FXUSDCAD", out var series) ||
				!series.TryGetProperty("v", out var value) ||
				!decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
				rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRate(date, NativeCurrency, fromCurrency, rate, Code));
		}

		return rates;
	}
}
