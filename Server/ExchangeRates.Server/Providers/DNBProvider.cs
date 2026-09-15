
using System.Globalization;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Danmarks Nationalbank
/// </summary>
public sealed class DNBProvider : CentralBankProviderBase {
	private readonly ILogger<DNBProvider> _logger;
	public DNBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<DNBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		var uri = $"{Url}/DNVALD/CSV?VALUTA={quoteCurrency}&KURSTYPE=100&Tid={fromDate:yyyy-MM-dd}-{toDate:yyyy-MM-dd}";
		using var response = await Http.GetAsync(uri, ct);
		response.EnsureSuccessStatusCode();

		var csv = await response.Content.ReadAsStringAsync(ct);
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (var line in lines.Skip(1)) {
			var values = line.Split(';');

			if (values.Length < 2 || !DateOnly.TryParse(values[0].Trim('"'), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			var value = values[^1].Trim().Trim('"').Replace(',', '.');

			if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate)) {
				continue;
			}

			rate /= 100m;

			results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
		}

		return results;
	}
}
