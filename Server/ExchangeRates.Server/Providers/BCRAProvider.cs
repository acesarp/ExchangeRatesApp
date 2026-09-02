using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de la República Argentina
/// </summary>
public sealed class BCRAProvider : CentralBankProviderBase {
	private readonly ILogger<BCRAProvider> _logger;

	public BCRAProvider(HttpClient http, IConfiguration configuration, ILogger<BCRAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCRA";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (quoteCurrency != ECurrencyISO.USD) {
			return [];
		}

		var results = new List<ExchangeRateResult>();

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var uri = $"{Url}?fechaCotizacion={date:yyyy-MM-dd}";
				var json = await Http.GetStringAsync(uri, ct);
				using var doc = JsonDocument.Parse(json);

				if (!doc.RootElement.TryGetProperty("results", out var resultsElement) || !resultsElement.TryGetProperty("detalle", out var detalle) || detalle.ValueKind != JsonValueKind.Array) {
					continue;
				}

				foreach (var item in detalle.EnumerateArray()) {
					if (!item.TryGetProperty("codigoMoneda", out var codeProp) || !codeProp.GetString()!.Equals(quoteCurrency.ToString(), StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					var rate = GetDecimal(item, "tipoCotizacion");

					if (rate <= 0) {
						continue;
					}

					results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
				}
			} catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to fetch BCRA rate for {Date}", date);
			}
		}

		return results;
	}
}
