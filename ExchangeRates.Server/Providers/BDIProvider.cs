using ExchangeRates.Server;
using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Providers;

using System.Globalization;
using System.Text.Json;

public sealed class BDIProvider : CentralBankProviderBase {
	public BDIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BDI";
	public override string Name => "Banca d'Italia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.EUR;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(
		ECurrencyISO fromCurrency,
		DateOnly fromDate,
		DateOnly toDate,
		CancellationToken ct) {

		var url =
			$"{Url.TrimEnd('/')}/dailyTimeSeries" +
			$"?startDate={fromDate:yyyy-MM-dd}" +
			$"&endDate={toDate:yyyy-MM-dd}" +
			$"&baseCurrencyIsoCode={NativeCurrency}" +
			$"&currencyIsoCode={fromCurrency}" +
			$"&lang=en";

		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Accept.ParseAdd("application/json");

		using var response = await Http.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		if (!doc.RootElement.TryGetProperty("rates", out var observations)) {
			return [];
		}

		var rates = new List<ExchangeRate>();

		foreach (var observation in observations.EnumerateArray()) {
			var dateText = observation.TryGetProperty("referenceDate", out var d) ? d.GetString() : null;
			var rate = GetDecimal(observation, "avgRate");

			if (!DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRate(
				date,
				NativeCurrency,
				fromCurrency,
				rate,
				Code));
		}

		return rates;
	}
}