using ExchangeRates.Domain.Enums;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque Centrale des Etats de l'Afrique de l'Ouest
/// </summary>
public sealed class BCEAOProvider : CentralBankProviderBase {
	private readonly ILogger<BCEAOProvider> _logger;

	public BCEAOProvider(HttpClient http, IConfiguration configuration, ILogger<BCEAOProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCEAO";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var uri = $"{Url}?date={date:yyyy-MM-dd}";
				var json = await Http.GetStringAsync(uri, ct);
				using var doc = JsonDocument.Parse(json);

				var root = doc.RootElement;
				var items = root.ValueKind == JsonValueKind.Array ? root : root.TryGetProperty("data", out var data) ? data : default;

				if (items.ValueKind != JsonValueKind.Array) {
					continue;
				}

				foreach (var item in items.EnumerateArray()) {
					if (!item.TryGetProperty("devise", out var currencyProp) || !currencyProp.GetString()!.Equals(quoteCurrency.ToString(), StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					var rate = GetDecimal(item, "cours");

					if (rate <= 0) {
						continue;
					}

					results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
				}
			}
			catch (Exception ex) {
				_logger.LogWarning(ex, "Failed to fetch BCEAO rate for {Date}", date);
			}
		}

		return results;
	}
}
