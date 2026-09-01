using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Uzbekistan
/// </summary>
public sealed class CBUProvider : CentralBankProviderBase {
	private readonly ILogger<CBUProvider> _logger;

	public CBUProvider(HttpClient http, IConfiguration configuration, ILogger<CBUProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBU";
	public override string Name => "Central Bank of Uzbekistan";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.UZS;

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var json = await Http.GetStringAsync(Url, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (doc.RootElement.ValueKind != JsonValueKind.Array) {
				return results;
			}

			var currencyCode = quoteCurrency.ToString();

			foreach (var item in doc.RootElement.EnumerateArray()) {
				var code = item.TryGetProperty("Ccy", out var c) ? c.GetString() : null;

				if (!string.Equals(code, currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!item.TryGetProperty("Date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "Rate");
				var nominal = GetDecimal(item, "Nominal");
				nominal = nominal <= 0 ? 1m : nominal;

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate / nominal, Code));
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBU rates.");
			return [];
		}
	}
}
