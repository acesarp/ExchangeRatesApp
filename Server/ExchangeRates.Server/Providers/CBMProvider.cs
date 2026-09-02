using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Myanmar
/// </summary>
public sealed class CBMProvider : CentralBankProviderBase {
	private readonly ILogger<CBMProvider> _logger;
	public CBMProvider(HttpClient http, IConfiguration configuration, ILogger<CBMProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBM";
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var json = await Http.GetStringAsync(Url, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();

			if (!doc.RootElement.TryGetProperty("rates", out var rates)) {
				return results;
			}

			var currencyCode = quoteCurrency.ToString();

			if (!rates.TryGetProperty(currencyCode, out var rateProp) || rateProp.ValueKind != JsonValueKind.String
				|| !decimal.TryParse(rateProp.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
				return results;
			}

			var dateStr = doc.RootElement.TryGetProperty("timestamp", out var t) ? t.GetString() : null;
			var date = DateOnly.TryParse(dateStr, out var parsedDate) ? parsedDate : DateOnly.FromDateTime(DateTime.UtcNow);

			if (date < fromDate || date > toDate) {
				return results;
			}

			results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBM rates.");
			return [];
		}
	}
}
