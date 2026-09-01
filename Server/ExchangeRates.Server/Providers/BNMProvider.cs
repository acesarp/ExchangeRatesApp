using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank Negara Malaysia
/// </summary>
public sealed class BNMProvider : CentralBankProviderBase {
	private readonly ILogger<BNMProvider> _logger;

	public BNMProvider(HttpClient http, IConfiguration configuration, ILogger<BNMProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BNM";
	public override string Name => "Bank Negara Malaysia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MYR;

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var uri = $"{Url}/{quoteCurrency}?session_time=1200&date={date:yyyy-MM-dd}";
				using var request = new HttpRequestMessage(HttpMethod.Get, uri);
				request.Headers.Add("Accept", "application/vnd.BNM.API.v1+json");

				using var response = await Http.SendAsync(request, ct);

				if (!response.IsSuccessStatusCode) {
					continue;
				}

				var json = await response.Content.ReadAsStringAsync(ct);
				using var doc = JsonDocument.Parse(json);

				if (!doc.RootElement.TryGetProperty("data", out var data)) {
					continue;
				}

				decimal rate;

				if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0) {
					rate = GetDecimal(data[0], "rate");
				} else if (data.ValueKind == JsonValueKind.Object) {
					rate = GetDecimal(data, "rate");
				} else {
					continue;
				}

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			} catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to fetch BNM rate for {Date}", date);
			}
		}

		return results;
	}
}
