
using System.Globalization;
using System.Text.Json;

using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Canada
/// </summary>
public sealed class BOCProvider : CentralBankProviderBase {
	private readonly ILogger<BOCProvider> _logger;
	public BOCProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BOCProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	/// <inheritdoc/>
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var url = $"{Url}?start_date={fromDate:yyyy-MM-dd}&end_date={toDate:yyyy-MM-dd}";
		using var doc = JsonDocument.Parse(await Http.GetStringAsync(url, ct));

		if (!doc.RootElement.TryGetProperty("observations", out var observations) || observations.GetArrayLength() == 0) {
			return [];
		}

		var rates = new List<ExchangeRateResult>();

		foreach (var observation in observations.EnumerateArray()) {
			if (!observation.TryGetProperty("d", out var dateValue) ||
				!DateOnly.TryParse(dateValue.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
				!observation.TryGetProperty("FXUSDCAD", out var series) ||
				!series.TryGetProperty("v", out var value) ||
				!decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
				rate <= 0) {
				continue;
			}

			rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, rate, Bank.BankCode));
		}

		return rates;
	}
}

