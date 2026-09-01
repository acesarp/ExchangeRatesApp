using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Botswana
/// </summary>
public sealed class BOBProvider : CentralBankProviderBase {
	private readonly ILogger<BOBProvider> _logger;

	public BOBProvider(HttpClient http, IConfiguration configuration, ILogger<BOBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOB";
	public override string Name => "Bank of Botswana";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.BWP;
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		try {
			var csv = await Http.GetStringAsync(Url, ct);
			var results = new List<ExchangeRateResult>();
			var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			if (lines.Length < 2) {
				return results;
			}

			var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
			var currencyIndex = Array.FindIndex(headers, h => h.Equals(quoteCurrency.ToString(), StringComparison.OrdinalIgnoreCase));
			var dateIndex = Array.FindIndex(headers, h => h.Contains("date", StringComparison.OrdinalIgnoreCase));

			if (currencyIndex < 0 || dateIndex < 0) {
				return results;
			}

			for (var i = 1; i < lines.Length; i++) {
				var cols = lines[i].Split(',').Select(c => c.Trim().Trim('"')).ToArray();

				if (cols.Length <= Math.Max(currencyIndex, dateIndex)) {
					continue;
				}

				if (!DateOnly.TryParse(cols[dateIndex], out var date) || date < fromDate || date > toDate) {
					continue;
				}

				if (!decimal.TryParse(cols[currencyIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				results.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}

			return results;
		} catch (Exception ex) {
			_logger.LogWarning(ex, "Failed to fetch BOB rates.");
			return [];
		}
	}
}
