
using System.Globalization;

using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Danmarks Nationalbank
/// </summary>
public sealed class DNBProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<DNBProvider> _logger;
	public DNBProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<DNBProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		// StatBank rejects time values that don't exist (weekends, future dates), so select whole months and filter afterwards.
		var months = new List<string>();
		for (var month = new DateOnly(fromDate.Year, fromDate.Month, 1); month <= toDate; month = month.AddMonths(1)) {
			months.Add($"{month:yyyy}M{month:MM}*");
		}

		// KURTYP=KBH is the Danmarks Nationalbank reference rate, quoted in DKK per 100 units.
		var uri = $"{ApiUrl}/DNVALD/CSV?VALUTA={quoteCurrency}&KURTYP=KBH&Tid={string.Join(',', months)}";
		using var response = await Http.GetAsync(uri, ct);
		response.EnsureSuccessStatusCode();
		var csv = await response.Content.ReadAsStringAsync(ct);
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		// Columns: VALUTA;KURTYP;TID;INDHOLD (TID is formatted as 2026M09D29)
		foreach (var line in lines.Skip(1)) {
			var values = line.Split(';');

			if (values.Length < 4 || !DateOnly.TryParseExact(values[2], "yyyy'M'MM'D'dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			if (date < fromDate || date > toDate) {
				continue;
			}

			var value = values[3].Trim().Replace(',', '.');

			if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
				continue;
			}

			rate /= 100m;
			results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
		}

		return results;
	}
}
