using ExchangeRates.Domain.Enums;

using HtmlAgilityPack;

using System.Globalization;
using System.Net;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque Centrale de Tunisie
/// </summary>
public sealed class BCTProvider : CentralBankProviderBase {
	private readonly ILogger<BCTProvider> _logger;

	public BCTProvider(HttpClient http, IConfiguration configuration, ILogger<BCTProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCT";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var uri = $"{Url}?date_debut={fromDate:dd/MM/yyyy}&date_fin={toDate:dd/MM/yyyy}";
			using var response = await Http.GetAsync(uri, ct);

			if (!response.IsSuccessStatusCode) {
				return [];
			}

			var html = await response.Content.ReadAsStringAsync(ct);
			var document = new HtmlDocument();
			document.LoadHtml(html);

			var results = new List<ExchangeRateResult>();
			var rows = document.DocumentNode.SelectNodes("//table//tr");

			if (rows is null) {
				return results;
			}

			var currencyCode = quoteCurrency.ToString();

			foreach (var row in rows) {
				var cells = row.SelectNodes("./td");

				if (cells is null || cells.Count < 3) {
					continue;
				}

				var values = cells.Select(c => WebUtility.HtmlDecode(c.InnerText).Trim()).ToArray();

				if (!DateOnly.TryParseExact(values[0], "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
					continue;
				}

				if (date < fromDate || date > toDate) {
					continue;
				}

				if (!values[1].Equals(currencyCode, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				if (!decimal.TryParse(values[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				if (InverseProvider) {
					rate = 1m / rate;
				}
				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		}
		catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BCT rates.");
			return [];
		}
	}
}
