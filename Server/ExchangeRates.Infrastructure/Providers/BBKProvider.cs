using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;
using ExchangeRates.Domain.Utilities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Globalization;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Deutsche Bundesbank
/// </summary>
public sealed class BBKProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.ForeignPerNative;

	private readonly ILogger<BBKProvider> _logger;
	public BBKProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BBKProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		// BBEX3.D.{currency}.EUR.BB.AC.000 = ECB euro reference rate (1 EUR = x currency). The configured ApiUrl ends with a
		// series key; replace it with the key for the requested currency.
		var flowUrl = ApiUrl[..(ApiUrl.IndexOf("/BBEX3", StringComparison.OrdinalIgnoreCase) + "/BBEX3".Length)];
		var url = $"{flowUrl}/D.{quoteCurrency.ToUpperInvariant()}.EUR.BB.AC.000?startPeriod={fromDate:yyyy-MM-dd}&endPeriod={toDate:yyyy-MM-dd}&format=csv";

		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Accept.ParseAdd("text/csv");
		using var response = await Http.SendAsync(request, ct);

		if (response.StatusCode == System.Net.HttpStatusCode.NotFound) {
			return [];
		}

		response.EnsureSuccessStatusCode();
		var csv = await response.Content.ReadAsStringAsync(ct);
		var rates = new List<ExchangeRateResult>();

		// The CSV starts with metadata rows. The layout follows Accept-Language: German rows look like "2026-09-29;1,1355;",
		// English rows like "2026-09-29,1.1355,".
		foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
			if (line.Length < 12 || !DateOnly.TryParseExact(line[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			// The character after the date is the column separator: ';' means German formatting (decimal comma).
			var german = line[10] == ';';
			var columns = line.Split(line[10]);
			var value = german ? columns[1].Replace(',', '.') : columns[1];

			if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
		}

		return rates;
	}

}

