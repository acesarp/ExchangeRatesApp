
using ExchangeRates.Domain.Entities;

using HtmlAgilityPack;

using System.Globalization;
using System.Net;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Financial Benchmarks India
/// </summary>
public sealed class FBILProvider : CentralBankProviderBase {
	private readonly ILogger<FBILProvider> _logger;

	public FBILProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<FBILProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var uri = $"{Url}?fromDate={fromDate:dd-MM-yyyy}&toDate={toDate:dd-MM-yyyy}";
		using var response = await Http.GetAsync(uri, ct);
		response.EnsureSuccessStatusCode();

		var html = await response.Content.ReadAsStringAsync(ct);
		return ExtractTable(html, quoteCurrency, fromDate, toDate);
	}

	private IReadOnlyList<ExchangeRateResult> ExtractTable(string html, string quoteCurrency, DateOnly fromDate, DateOnly toDate) {
		var document = new HtmlDocument();
		document.LoadHtml(html);

		var results = new List<ExchangeRateResult>();
		var rows = document.DocumentNode.SelectNodes("//table//tr");

		if (rows is null) {
			return results;
		}

		foreach (var row in rows) {
			var cells = row.SelectNodes("./td");

			if (cells is null || cells.Count < 4) {
				continue;
			}

			var values = cells.Select(cell => WebUtility.HtmlDecode(cell.InnerText).Trim()).ToArray();

			if (!DateOnly.TryParseExact(values[0], "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
				continue;
			}

			if (date < fromDate || date > toDate) {
				continue;
			}

			if (!TryGetCurrency(values[2], out var currency, out var unitMultiplier)) {
				continue;
			}

			if (currency != quoteCurrency) {
				continue;
			}

			if (!decimal.TryParse(values[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var rate)) {
				continue;
			}

			rate /= unitMultiplier;

			results.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
		}

		return results;
	}

	private static bool TryGetCurrency(string currencyPair, out string currency, out decimal multiplier) {
		currency = default;
		multiplier = 1m;

		return currencyPair switch {
			"INR / 1 USD" => Set("USD", 1m, out currency, out multiplier),
			"INR / 1 GBP" => Set("GBP", 1m, out currency, out multiplier),
			"INR / 1 EUR" => Set("EUR", 1m, out currency, out multiplier),
			"INR / 100 JPY" => Set("JPY", 100m, out currency, out multiplier),
			"INR / 1 AED" => Set("AED", 1m, out currency, out multiplier),
			"INR / 10000 IDR" => Set("IDR", 10_000m, out currency, out multiplier),
			_ => false
		};
	}

	private static bool Set(string value, decimal valueMultiplier, out string currency, out decimal multiplier) {
		currency = value;
		multiplier = valueMultiplier;
		return true;
	}
}
