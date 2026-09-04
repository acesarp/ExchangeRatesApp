using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Utilities;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of England.
/// Retrieves daily spot exchange rates against Pound Sterling (GBP).
/// </summary>
public sealed class BOEProvider : CentralBankProviderBase {
	private readonly ILogger<BOEProvider> _logger;
	private readonly Dictionary<string, string> _series;
	public BOEProvider(HttpClient http, IConfiguration configuration, ILogger<BOEProvider> logger) : base(http, configuration) {
		_logger = logger;
		_series = configuration.GetSection($"CentralBanks:{Code}:Series")
											.GetChildren()
											.ToDictionary(x => x.Key, x => x.Value!, StringComparer.OrdinalIgnoreCase);
	}

	public override string Code => "BOE";

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (!_series.TryGetValue(quoteCurrency.ToString(), out var quoteCode)) {
			_logger.LogWarning("Series code not found for quote currency: {QuoteCurrency}", quoteCurrency);
			return [];
		}

		var url = $"{Url}?CodeVer=new&xml.x=yes" +
	$"&Datefrom={Uri.EscapeDataString(fromDate.ToString("dd/MMM/yyyy", CultureInfo.InvariantCulture))}" +
	$"&Dateto={Uri.EscapeDataString(toDate.ToString("dd/MMM/yyyy", CultureInfo.InvariantCulture))}" +
	$"&SeriesCodes={quoteCode}" +
	"&VPD=Y";

		var csv = await Http.GetStringAsync(url, ct);

		Console.WriteLine(url);
		Console.WriteLine(csv);

		var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		if (lines.Length < 2) {
			return [];
		}

		var headers = TextUtils.SplitCsv(lines[0]);
		var rates = new List<ExchangeRateResult>();
		var dateIndex = headers.FindIndex(x => x.Equals("DATE", StringComparison.OrdinalIgnoreCase));
		var valueIndex = headers.FindIndex(x => x.Equals(quoteCode, StringComparison.OrdinalIgnoreCase));

		foreach (var line in lines.Skip(1)) {
			var values = TextUtils.SplitCsv(line);
			if (dateIndex < 0 || valueIndex < 0 || dateIndex >= values.Count || valueIndex >= values.Count) {
				continue;
			}

			if (!DateOnly.TryParse(values[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
				!decimal.TryParse(values[valueIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
				rate <= 0) {
				continue;
			}

			if (InverseProvider) {
				rate = 1m / rate;
			}
			rates.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, 1m / rate, Code));
		}
		return rates;
	}
}
