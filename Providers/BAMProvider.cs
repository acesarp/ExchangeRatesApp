using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank Al-Maghrib
/// </summary>
public sealed class BAMProvider : CentralBankProviderBase {
	public BAMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BAM";
	public override string Name => "Bank Al-Maghrib";
	public override ECurrency NativeCurrency => ECurrency.MAD;

	protected override async Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrency fromCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
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
		var rates = new List<ExchangeRate>();
		Console.WriteLine(rawJson);
		foreach (var row in doc.RootElement.EnumerateArray()) {
			var code = row.TryGetProperty("libDevise", out var c) ? c.GetString() : null;
			if (string.IsNullOrWhiteSpace(code)) {
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

			rates.Add(new ExchangeRate(from, NativeCurrency, Enum.Parse<ECurrency>(code), mid / unit, Code));
		}
		return rates;
	}
}
