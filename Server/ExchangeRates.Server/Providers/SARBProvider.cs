using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// South African Reserve Bank
/// </summary>
public sealed class SARBProvider : CentralBankProviderBase {
	private readonly ILogger<SARBProvider> _logger;

	public SARBProvider(HttpClient http, IConfiguration configuration, ILogger<SARBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "SARB";
	public override string Name => "South African Reserve Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.ZAR;
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var currencyCode = quoteCurrency.ToString();
			var uri = $"{Url}?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&currency={currencyCode}";
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			var results = new List<ExchangeRateResult>();
			var root = doc.RootElement;
			var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

			if (items.ValueKind != JsonValueKind.Array) {
				return results;
			}

			foreach (var item in items.EnumerateArray()) {
				if (!item.TryGetProperty("Date", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var rate = GetDecimal(item, "Value");

				if (rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch SARB rates.");
			return [];
		}
	}
}
