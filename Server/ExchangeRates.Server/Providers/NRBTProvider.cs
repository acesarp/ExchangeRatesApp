using ExcelDataReader;

using ExchangeRates.Domain.Entities;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Reserve Bank of Tonga
/// </summary>
public sealed class NRBTProvider : CentralBankProviderBase {
	private readonly ILogger<NRBTProvider> _logger;

	public NRBTProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<NRBTProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

		try {
			using var response = await Http.GetAsync($"{Url}/data/docs/fmarkets/exrates/average_daily_exchange_rates.xlsx", ct);
			response.EnsureSuccessStatusCode();

			await using var stream = await response.Content.ReadAsStreamAsync(ct);
			using var reader = ExcelReaderFactory.CreateReader(stream);

			do {
				int? quoteColumn = null;
				var insideMidSection = false;

				while (reader.Read()) {
					// Locate the MID section.
					if (!insideMidSection) {
						for (var col = 0; col < reader.FieldCount; col++) {
							var value = reader.GetValue(col)?.ToString()?.Trim();

							if (value?.Contains("MID", StringComparison.OrdinalIgnoreCase) == true) {
								insideMidSection = true;
								break;
							}
						}
						continue;
					}

					// Locate the requested currency inside the MID header.
					if (quoteColumn == null) {
						for (var col = 1; col < reader.FieldCount; col++) {
							var header = reader.GetValue(col)?.ToString()?.Trim();

							if (string.Equals(header, quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
								quoteColumn = col;
								break;
							}
						}

						continue;
					}

					if (!TryExtractDate(reader.GetValue(0), out var date)) {
						continue;
					}

					if (date < fromDate || date > toDate) {
						continue;
					}

					var rawRate = reader.GetValue(quoteColumn.Value);

					if (!TryExtractRate(rawRate, out var rate)) {
						continue;
					}

					results.Add(new ExchangeRateResult(
						Date: date,
						BaseCurrency: "TOP",
						QuoteCurrency: quoteCurrency,
						Rate: rate,
						Provider: Bank.BankCode
					));
				}
			}
			while (reader.NextResult());
		}
		catch (Exception ex) {
			_logger.LogError(ex, "[{Provider}] Error fetching exchange rates", Bank.BankCode);
		}

		return results.OrderBy(x => x.Date)
			.DistinctBy(x => x.Date)
			.ToList();
	}

	private static bool TryExtractDate(object? source, out DateOnly date) {
		switch (source) {
			case DateTime dt:
				date = DateOnly.FromDateTime(dt);
				return true;

			case double serialDate:
				date = DateOnly.FromDateTime(DateTime.FromOADate(serialDate));
				return true;

			case string value when DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed):
				date = DateOnly.FromDateTime(parsed);
				return true;

			default:
				date = default;
				return false;
		}
	}

	private static bool TryExtractRate(object? source, out decimal rate) {
		switch (source) {
			case double value:
				rate = (decimal)value;
				return true;

			case decimal value:
				rate = value;
				return true;

			default:
				return decimal.TryParse(source?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out rate);
		}
	}
}
