
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Text.Json;
using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Banco de la República
/// </summary>
public sealed class BANREPProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BANREPProvider> _logger;
	public BANREPProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BANREPProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		// The dataset is the TRM (Tasa Representativa del Mercado): COP per USD only.
		if (quoteCurrency != "USD") {
			return [];
		}

		var url = $"{ApiUrl}?$where=vigenciadesde >= '{fromDate:yyyy-MM-dd}T00:00:00.000' AND vigenciadesde <= '{toDate:yyyy-MM-dd}T00:00:00.000'&$order=vigenciadesde";
		using var response = await Http.GetAsync(url, ct);
		response.EnsureSuccessStatusCode();

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

		if (doc.RootElement.ValueKind != JsonValueKind.Array) {
			return [];
		}

		var rates = new List<ExchangeRateResult>();

		// [{ "valor": "3349.63", "vigenciadesde": "2026-09-29T00:00:00.000", "vigenciahasta": "2026-09-29T00:00:00.000" }]
		foreach (var row in doc.RootElement.EnumerateArray()) {
			if (!row.TryGetProperty("vigenciadesde", out var dateProp) || !DateTime.TryParse(dateProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime)) {
				continue;
			}

			var rate = GetDecimal(row, "valor");

			if (rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(DateOnly.FromDateTime(dateTime), Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
		}

		return rates;
	}
}

