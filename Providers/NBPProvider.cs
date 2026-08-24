using ExchangeRates.Server.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Narodowy Bank Polski
/// </summary>
public sealed class NBPProvider : CentralBankProviderBase {
	public NBPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBP";
	public override string Name => "Narodowy Bank Polski";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.PLN;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrencyISO fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url.TrimEnd('/')}/tables/A/{fromDate:yyyy-MM-dd}?format=json";
		using var response = await Http.GetAsync(url, ct);

		if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
			return [];
		}

		response.EnsureSuccessStatusCode();
		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		var rates = new List<ExchangeRate>();
		var table = doc.RootElement[0];

		foreach (var row in table.GetProperty("rates").EnumerateArray()) {
			var code = row.GetProperty("code").GetString();
			var rate = GetDecimal(row, "mid");

			if (!string.IsNullOrWhiteSpace(code) && rate > 0) {
				rates.Add(new ExchangeRate(fromDate, NativeCurrency, Enum.Parse<ECurrencyISO>(code!), rate, Code));
			}
		}
		return rates;
	}
}
