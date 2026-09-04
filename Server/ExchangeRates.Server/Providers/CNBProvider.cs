using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Czech National Bank
/// </summary>
public sealed class CNBProvider : CentralBankProviderBase {
	private readonly ILogger<CNBProvider> _logger;

	public CNBProvider(HttpClient http, IConfiguration configuration, ILogger<CNBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CNB";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var results = new List<ExchangeRateResult>();
			var currencyCode = quoteCurrency.ToString();

			for (var year = fromDate.Year; year <= toDate.Year; year++) {
				var uri = $"{Url}?year={year}";
				var json = await Http.GetStringAsync(uri, ct);
				using var doc = JsonDocument.Parse(json);

				if (!doc.RootElement.TryGetProperty("rates", out var rates) || rates.ValueKind != JsonValueKind.Array) {
					continue;
				}

				foreach (var item in rates.EnumerateArray()) {
					var code = item.TryGetProperty("currencyCode", out var c) ? c.GetString() : null;

					if (!string.Equals(code, currencyCode, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					if (!item.TryGetProperty("validFor", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
						continue;
					}

					if (date < fromDate || date > toDate) {
						continue;
					}

					var rate = GetDecimal(item, "rate");
					var amount = GetDecimal(item, "amount");
					amount = amount <= 0 ? 1m : amount;

					if (rate <= 0) {
						continue;
					}

					if (InverseProvider) {
						rate = 1m / rate;
					}
					results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate / amount, Code));
				}
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CNB rates.");
			return [];
		}
	}
}
