using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Text;
namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central del Paraguay
/// </summary>
public sealed class BCPProvider : CentralBankProviderBase {
	private readonly ILogger<BCPProvider> _logger;
	public BCPProvider(HttpClient http, IConfiguration configuration, ILogger<BCPProvider> logger) : base(http, configuration) {
		_logger = logger;
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
	}

	public override string Code => "BCP";
	public override string Name => "Banco Central del Paraguay";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.PYG;

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var results = new List<ExchangeRateResult>();

		for (var year = fromDate.Year; year <= toDate.Year; year++) {
			using var content = new FormUrlEncodedContent(new Dictionary<string, string> {
				["anho"] = year.ToString(),
				["moneda"] = quoteCurrency.ToString()
			});

			using var response = await Http.GetAsync(Url + $"/xls?anho={year}&moneda={quoteCurrency}", ct);
			response.EnsureSuccessStatusCode();

			//await using var stream = await response.Content.ReadAsStreamAsync(ct);
			//using var reader = ExcelReaderFactory.CreateReader(stream);

			var contentType = response.Content.Headers.ContentType?.ToString();
			var bytes = await response.Content.ReadAsByteArrayAsync(ct);
			var text = Encoding.UTF8.GetString(bytes);

			_logger.LogInformation("BCP Content-Type: {ContentType}", contentType);
			_logger.LogInformation("BCP Response Length: {Length}", bytes.Length);
			_logger.LogInformation("BCP Response: {Response}", text[..Math.Min(text.Length, 500)]);

			/*
			while (reader.Read()) {
				if (!TryGetDay(reader.GetValue(0), out var day)) {
					continue;
				}

				for (var month = 1; month <= 12; month++) {
					if (month >= reader.FieldCount) {
						continue;
					}

					if (!DateOnly.TryParseExact($"{year}-{month:D2}-{day:D2}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) {
						continue;
					}

					if (date < fromDate || date > toDate) {
						continue;
					}

					if (!TryGetDecimal(reader.GetValue(month), out var pygPerCurrency)) {
						continue;
					}

					if (pygPerCurrency <= 0) {
						continue;
					}

					var rate = 1m / pygPerCurrency;

					results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
				}
			}
		*/
		}

		return results.OrderBy(x => x.Date).ToList();
	}

	private static bool TryGetDay(object? value, out int day) {
		day = 0;
		return value != null && int.TryParse(value.ToString(), out day) && day is >= 1 and <= 31;
	}

	private static bool TryGetDecimal(object? value, out decimal result) {
		result = 0;

		if (value == null) {
			return false;
		}

		if (value is double doubleValue) {
			result = (decimal)doubleValue;
			return true;
		}

		var text = value.ToString()?.Trim();

		if (string.IsNullOrWhiteSpace(text) || text.Equals("ND", StringComparison.OrdinalIgnoreCase)) {
			return false;
		}

		return decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("es-PY"), out result);
	}
}
