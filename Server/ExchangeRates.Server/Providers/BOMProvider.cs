using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Mongolia
/// </summary>
public sealed class BOMProvider : CentralBankProviderBase {
	private readonly ILogger<BOMProvider> _logger;

	public BOMProvider(HttpClient http, IConfiguration configuration, ILogger<BOMProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOM";
	public override string Name => "Bank of Mongolia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MNT;
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var uri = $"{Url}?date_from={fromDate:yyyy-MM-dd}&date_to={toDate:yyyy-MM-dd}&code={quoteCurrency}";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();
			var root = doc.RootElement;
			var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

			if (items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in items.EnumerateArray()) {
				if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "rate");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BOM rates.");
			return [];
		}
	}
}
