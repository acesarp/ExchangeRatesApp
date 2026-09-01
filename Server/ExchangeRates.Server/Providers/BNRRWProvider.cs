using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque Nationale du Rwanda
/// </summary>
public sealed class BNRRWProvider : CentralBankProviderBase {
	private readonly ILogger<BNRRWProvider> _logger;

	public BNRRWProvider(HttpClient http, IConfiguration configuration, ILogger<BNRRWProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BNRRW";
	public override string Name => "Banque Nationale du Rwanda";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.RWF;

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var uri = $"{Url}?currency={quoteCurrency}&from={fromDate:yyyy-MM-dd}&to={toDate:yyyy-MM-dd}";
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
			_logger.LogWarning(ex, "Failed to fetch BNRRW rates.");
			return [];
		}
	}
}
