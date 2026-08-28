using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco de México
/// </summary>
public sealed class BANXICOProvider : CentralBankProviderBase {
	public BANXICOProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BANXICO";
	public override string Name => "Banco de México";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MXN;

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (string.IsNullOrWhiteSpace(ApiKey)) {
			throw new InvalidOperationException("Missing CentralBanks:BANXICO:ApiKey.");
		}

		// Example: USD/MXN FIX exchange rate series.
		const string seriesId = "SF43718";
		var url = $"{Url.TrimEnd('/')}/{seriesId}/datos/{fromDate:yyyy-MM-dd}/{toDate:yyyy-MM-dd}";

		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Add("Bmx-Token", ApiKey);

		using var response = await Http.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		var series = doc.RootElement.GetProperty("bmx")
																		.GetProperty("series")[0];

		if (!series.TryGetProperty("datos", out var data) || data.GetArrayLength() == 0) {
			return [];
		}

		var value = data[0].GetProperty("dato").GetString();

		if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate)) {
			return [];
		}

		return [   new ExchangeRateResult(fromDate,  NativeCurrency,quoteCurrency,rate,Code)
		];
	}
}
