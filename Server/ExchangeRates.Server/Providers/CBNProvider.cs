using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Nigeria
/// </summary>
public sealed class CBNProvider : CentralBankProviderBase {
	private readonly ILogger<CBNProvider> _logger;

	public CBNProvider(HttpClient http, IConfiguration configuration, ILogger<CBNProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBN";
	public override string Name => "Central Bank of Nigeria";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.NGN;
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var json = await Http.GetStringAsync(Url, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();
			var root = doc.RootElement;
			var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

			if (items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			var currencyCode = quoteCurrency.ToString();
			var today = DateOnly.FromDateTime(DateTime.UtcNow);

			if (today < fromDate || today > toDate) {
				return results;
			}

			foreach (var item in items.EnumerateArray()) {
				var code = item.TryGetProperty("currency", out var c) ? c.GetString() : null;

				if (!string.Equals(code, currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				var rate = GetDecimal(item, "sellingRate");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(today, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch CBN rates.");
			return [];
		}
	}
}
