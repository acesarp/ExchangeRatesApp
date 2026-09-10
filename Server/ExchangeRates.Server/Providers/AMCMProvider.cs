
using ExchangeRates.Domain.Entities;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Monetary Authority of Macao
/// </summary>
public sealed class AMCMProvider : CentralBankProviderBase {
	private readonly ILogger<AMCMProvider> _logger;
	public AMCMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<AMCMProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?QueryType=1&Begin={fromDate:yyyyMMdd}&End={toDate:yyyyMMdd}";

		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));
		var rates = new List<ExchangeRateResult>();

		if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) {
			return rates;
		}

		foreach (var row in data.EnumerateArray()) {
			var Bank.Code = row.TryGetProperty("currency", out var c) ? c.GetString() : null;
			if (string.IsNullOrWhiteSpace(Bank.Code)) {
				continue;
			}

			var unit = GetDecimal(row, "unit");
			var value = GetDecimal(row, "usdMeanValue");
			if (unit <= 0 || value <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(fromDate, quoteCurrency!, Bank.Currency.Code, value / unit, Bank.Code));
		}
		return rates;
	}
}

