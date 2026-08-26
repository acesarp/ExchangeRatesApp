using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Monetary Authority of Macao
/// </summary>
public sealed class AMCMProvider : CentralBankProviderBase {
	public AMCMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "AMCM";
	public override string Name => "Monetary Authority of Macao";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MOP;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?QueryType=1&Begin={fromDate:yyyyMMdd}&End={toDate:yyyyMMdd}";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
		var rates = new List<ExchangeRate>();

		if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) {
			return rates;
		}

		foreach (var row in data.EnumerateArray()) {
			var code = row.TryGetProperty("currency", out var c) ? c.GetString() : null;
			if (string.IsNullOrWhiteSpace(code)) {
				continue;
			}

			var unit = GetDecimal(row, "unit");
			var value = GetDecimal(row, "usdMeanValue");
			if (unit <= 0 || value <= 0) {
				continue;
			}
			rates.Add(new ExchangeRate(fromDate, quoteCurrency!, NativeCurrency, value / unit, Code));
		}
		return rates;
	}
}
