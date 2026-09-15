using ExchangeRates.Domain.Entities;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Net;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Australia
/// </summary>
public sealed class RBAProvider : CentralBankProviderBase {
	private readonly ILogger<RBAProvider> _logger;

	public RBAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<RBAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		using var request = new HttpRequestMessage(HttpMethod.Get, Url);

		request.Version = HttpVersion.Version11;
		request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

		request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
		request.Headers.TryAddWithoutValidation("User-Agent", "PostmanRuntime/7.54.0");
		request.Headers.TryAddWithoutValidation("Accept", "*/*");
		request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
		request.Headers.TryAddWithoutValidation("Connection", "keep-alive");


		var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
		response.EnsureSuccessStatusCode();

		var csv = await response.Content.ReadAsStringAsync(ct);
		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length == 0) {
			_logger.LogWarning("No data found in the CSV response.");
			return [];
		}

		var titleLine = lines.FirstOrDefault(x => x.StartsWith("Title,", StringComparison.OrdinalIgnoreCase));
		var seriesIdIndex = Array.FindIndex(lines, x => x.StartsWith("Series ID,", StringComparison.OrdinalIgnoreCase));

		if (titleLine is null || seriesIdIndex < 0) {
			_logger.LogWarning("Title line or series ID not found in the CSV response.");
			return [];
		}

		var headers = TextUtils.SplitCsv(titleLine);
		var columnName = $"A$1={quoteCurrency}";
		var valueIndex = headers.FindIndex(x => x.Equals(columnName, StringComparison.OrdinalIgnoreCase));

		if (valueIndex < 0) {
			return [];
		}

		var rates = new List<ExchangeRateResult>();

		foreach (var line in lines.Skip(seriesIdIndex + 1)) {
			var values = TextUtils.SplitCsv(line);

			if (values.Count <= valueIndex) {
				continue;
			}

			if (!DateOnly.TryParseExact(values[0], "dd-MMM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
				date < fromDate ||
				date > toDate ||
				!decimal.TryParse(values[valueIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
				rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
		}
		return rates;
	}
}
