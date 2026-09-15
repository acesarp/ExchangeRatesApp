
using ExchangeRates.Domain.Entities;

using System.Globalization;
using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Hong Kong Monetary Authority
/// </summary>
public sealed class HKMAProvider : CentralBankProviderBase {
	private readonly ILogger<HKMAProvider> _logger;
	public HKMAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<HKMAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();
		var offset = 0;

		while (true) {
			var uri = $"{Url}?offset={offset}";
			using var response = await Http.GetAsync(uri, ct);
			response.EnsureSuccessStatusCode();

			using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
			var result = json.RootElement.GetProperty("result");
			var records = result.GetProperty("records");

			if (records.GetArrayLength() == 0) {
				break;
			}

			foreach (var record in records.EnumerateArray()) {
				if (!DateOnly.TryParseExact(record.GetProperty("end_of_day").GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				var property = GetCurrencyProperty(quoteCurrency);

				if (!record.TryGetProperty(property, out var rateElement) || rateElement.ValueKind == JsonValueKind.Null) {
					continue;
				}

				if (!rateElement.TryGetDecimal(out var rate)) {
					continue;
				}



				results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
			}

			var datasize = result.GetProperty("datasize").GetInt32();

			if (offset + records.GetArrayLength() >= datasize) {
				break;
			}

			offset += records.GetArrayLength();
		}
		return results;
	}

	private static string GetCurrencyProperty(string currency) => currency switch {
		"XDR" => "special_drawing_rights",
		_ => currency.ToLowerInvariant()
	};
}
