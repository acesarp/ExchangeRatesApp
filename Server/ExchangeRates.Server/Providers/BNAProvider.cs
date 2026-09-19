using ExchangeRates.Domain.Entities;

using System.Globalization;
using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Nacional de Angola
/// </summary>
public sealed class BNAProvider : CentralBankProviderBase {
	private readonly ILogger<BNAProvider> _logger;

	public BNAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BNAProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var url = $"{ApiUrl}?datainicio={fromDate:yyyy-MM-dd}&datafim={toDate:yyyy-MM-dd}&tipocambio=all&moeda={Uri.EscapeDataString(quoteCurrency)}";
			using HttpResponseMessage response = await Http.GetAsync(url, ct);
			response.EnsureSuccessStatusCode();
			using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
			if (!doc.RootElement.TryGetProperty("genericResponse", out var items) || items.ValueKind != JsonValueKind.Array) {
				return Array.Empty<ExchangeRateResult>();
			}

			var results = new List<ExchangeRateResult>();
			var today = DateOnly.FromDateTime(DateTime.UtcNow);

			foreach (var item in items.EnumerateArray()) {
				if (!item.TryGetProperty("codigoMoeda", out var currencyElement)) {
					continue;
				}

				if (!string.Equals(currencyElement.GetString(), quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!item.TryGetProperty("data", out var dateElement) || !DateOnly.TryParseExact(dateElement.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
					continue;
				}

				var publishedRate = GetDecimal(item, "taxa");
				if (publishedRate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, NativeCurrencyCode, quoteCurrency, 1m / publishedRate, BankCode));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BNA rates.");
			return Array.Empty<ExchangeRateResult>();
		}
	}
}
