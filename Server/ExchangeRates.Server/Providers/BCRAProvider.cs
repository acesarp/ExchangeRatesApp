
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de la República Argentina
/// </summary>
public sealed class BCRAProvider : CentralBankProviderBase {
	private readonly ILogger<BCRAProvider> _logger;

	public BCRAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCRAProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency != "USD") {
			return [];
		}

		var results = new List<ExchangeRateResult>();

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var uri = $"{ApiUrl}?fechaCotizacion={date:yyyy-MM-dd}";
				var json = await Http.GetStringAsync(uri, ct);
				using var doc = JsonDocument.Parse(json);

				if (!doc.RootElement.TryGetProperty("results", out var resultsElement) || !resultsElement.TryGetProperty("detalle", out var detalle) || detalle.ValueKind != JsonValueKind.Array) {
					continue;
				}

				foreach (var item in detalle.EnumerateArray()) {
					if (!item.TryGetProperty("codigoMoneda", out var codeProp) || !codeProp.GetString()!.Equals(quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					var rate = GetDecimal(item, "tipoCotizacion");

					if (rate <= 0) {
						continue;
					}



					results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				}
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to fetch BCRA rate for {Date}", date);
			}
		}

		return results;
	}
}

