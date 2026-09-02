using ExchangeRates.Server;
using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Providers;

using System.Globalization;
using System.Text.Json;

public sealed class BDIProvider : CentralBankProviderBase {
	private readonly ILogger<BDIProvider> _logger;

	public BDIProvider(HttpClient http, IConfiguration configuration, ILogger<BDIProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BDI";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (fromDate > toDate) {
			throw new ArgumentException("fromDate cannot be greater than toDate.");
		}

		if (quoteCurrency == NativeCurrency) {
			return [];
		}

		var url = $"{Url.TrimEnd('/')}/dailyTimeSeries" +
			$"?startDate={fromDate:yyyy-MM-dd}" +
			$"&endDate={toDate:yyyy-MM-dd}" +
			$"&baseCurrencyIsoCode={quoteCurrency}" +
			$"&currencyIsoCode={NativeCurrency}" +
			$"&lang=en";

		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Accept.ParseAdd("application/json");

		using var response = await Http.SendAsync(request, ct);

		var responseStr = await response.Content.ReadAsStringAsync(ct);
		_logger.LogInformation("Response from BDI: {Response}", responseStr);

		using var doc = JsonDocument.Parse(responseStr);

		if (!doc.RootElement.TryGetProperty("rates", out var observations)) {
			return [];
		}

		var rates = new List<ExchangeRateResult>();

		foreach (var observation in observations.EnumerateArray()) {
			var dateText = observation.TryGetProperty("referenceDate", out var d) ? d.GetString() : null;
			var rate = GetDecimal(observation, "avgRate");

			if (!DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
		}

		return rates;
	}
}