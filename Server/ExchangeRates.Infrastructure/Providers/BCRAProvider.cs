
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Text.Json;
using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Banco Central de la República Argentina
/// </summary>
public sealed class BCRAProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BCRAProvider> _logger;

	public BCRAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BCRAProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();
		var uri = $"{ApiUrl}/{quoteCurrency}?fechadesde={fromDate:yyyy-MM-dd}&fechahasta={toDate:yyyy-MM-dd}";

		try {
			var json = await Http.GetStringAsync(uri, ct);
			using var doc = JsonDocument.Parse(json);

			if (!doc.RootElement.TryGetProperty("results", out var resultsElement) || resultsElement.ValueKind != JsonValueKind.Array) {
				return results;
			}

			// results: [{ "fecha": "2026-10-02", "detalle": [{ "codigoMoneda": "USD", "tipoCotizacion": 1520.0 }] }]
			foreach (var day in resultsElement.EnumerateArray()) {
				if (!day.TryGetProperty("fecha", out var dateProp) || !DateOnly.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, out var date)) {
					continue;
				}

				if (!day.TryGetProperty("detalle", out var detalle) || detalle.ValueKind != JsonValueKind.Array) {
					continue;
				}

				foreach (var item in detalle.EnumerateArray()) {
					if (!item.TryGetProperty("codigoMoneda", out var codeProp) || !string.Equals(codeProp.GetString(), quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					var rate = GetDecimal(item, "tipoCotizacion");

					if (rate <= 0) {
						continue;
					}

					results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
				}
			}
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BCRA rates for {FromDate}-{ToDate}", fromDate, toDate);
		}

		return results;
	}
}

