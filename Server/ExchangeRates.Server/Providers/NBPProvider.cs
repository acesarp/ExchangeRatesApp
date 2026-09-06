using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Narodowy Bank Polski
/// </summary>
public sealed class NBPProvider : CentralBankProviderBase {
	private readonly ILogger<NBPProvider> _logger;

	public NBPProvider(HttpClient http, IConfiguration configuration, ILogger<NBPProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBP";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var url = $"{Url.TrimEnd('/')}/tables/A/{fromDate:yyyy-MM-dd}?format=json";
		using var response = await Http.GetAsync(url, ct);

		if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
			return [];
		}

		response.EnsureSuccessStatusCode();
		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		var rates = new List<ExchangeRateResult>();
		var table = doc.RootElement[0];

		foreach (var row in table.GetProperty("rates").EnumerateArray()) {
			var code = row.GetProperty("code").GetString();
			var rate = GetDecimal(row, "mid");

			if (!string.IsNullOrWhiteSpace(code) && rate > 0) {
				
					
				rates.Add(new ExchangeRateResult(fromDate, NativeCurrency, Enum.Parse<ECurrencyISO>(code!), rate, Code));
			}
		}
		return rates;
	}
}
