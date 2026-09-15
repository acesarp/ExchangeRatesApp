
using ExchangeRates.Domain.Entities;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Narodowy Bank Polski
/// </summary>
public sealed class NBPProvider : CentralBankProviderBase {
	private readonly ILogger<NBPProvider> _logger;

	public NBPProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NBPProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
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
			var bankCode = row.GetProperty("Bank.BankCode").GetString();
			var rate = GetDecimal(row, "mid");

			if (!string.IsNullOrWhiteSpace(bankCode) && rate > 0) {


				rates.Add(new ExchangeRateResult(fromDate, Bank.NativeCurrency.CurrencyCode, BankCode!, rate, bankCode));
			}
		}
		return rates;
	}
}

