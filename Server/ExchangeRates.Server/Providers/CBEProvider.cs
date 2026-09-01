using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Egypt
/// </summary>
public sealed class CBEProvider : CentralBankProviderBase {
	private readonly ILogger<CBEProvider> _logger;

	public CBEProvider(HttpClient http, IConfiguration configuration, ILogger<CBEProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBE";
	public override string Name => "Central Bank of Egypt";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.EGP;
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var uri = $"{Url}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();
			var root = doc.RootElement;
			var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

			if (items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			var currencyCode = quoteCurrency.ToString();

			foreach (var item in items.EnumerateArray()) {
				var code = item.TryGetProperty("currencyCode", out var c) ? c.GetString() : null;

				if (!string.Equals(code, currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!item.TryGetProperty("date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "sellPrice");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBE rates.");
			return [];
		}
	}
}
