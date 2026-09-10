
using ExchangeRates.Domain.Entities;

using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank Al-Maghrib
/// </summary>
public sealed class BAMProvider : CentralBankProviderBase {
	private readonly ILogger<BAMProvider> _logger;
	public BAMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BAMProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var apiKey = ApiKey;
		if (string.IsNullOrWhiteSpace(apiKey)) {
			throw new InvalidOperationException("Missing CentralBanks:BAM:ApiKey.");
		}

		var url = $"{Url.TrimEnd('/')}/cours/Version1/api/CoursVirement?date={fromDate:yyyy-MM-dd}T12:30:00";
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", apiKey);

		using var response = await Http.SendAsync(request, ct);
		response.EnsureSuccessStatusCode();

		var rawJson = await response.Content.ReadAsStringAsync(ct);
		using var doc = JsonDocument.Parse(rawJson);
		var rates = new List<ExchangeRateResult>();
		Console.WriteLine(rawJson);

		foreach (var row in doc.RootElement.EnumerateArray()) {
			var bankCode = row.TryGetProperty("libDevise", out var c) ? c.GetString() : null;
			if (string.IsNullOrWhiteSpace(bankCode)) {
				continue;
			}

			var unit = GetDecimal(row, "uniteDevise");
			var mid = GetDecimal(row, "moyen");
			if (mid <= 0) {
				var buy = GetDecimal(row, "achat");
				var sell = GetDecimal(row, "vente");
				if (buy > 0 && sell > 0) {
					mid = (buy + sell) / 2m;
				}
			}

			if (unit <= 0 || mid <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(fromDate, Bank.Currency.CurrencyCode, quoteCurrency, mid / unit, bankCode));
		}
		return rates;
	}
}

