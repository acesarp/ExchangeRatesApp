using ExchangeRates.Domain.Enums;

using System.Globalization;
using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Natsyyanalny Bank Respubliki Belarus
/// </summary>
public sealed class NBRBProvider : CentralBankProviderBase {
	private readonly ILogger<NBRBProvider> _logger;

	public NBRBProvider(HttpClient http, IConfiguration configuration, ILogger<NBRBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBRB";
	public override string Name => "Natsyyanalny Bank Respubliki Belarus";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.BYN;

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var rates = new List<ExchangeRateResult>();

		// NBRB API returns only the latest rate, so we fetch for a specific date
		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			try {
				var url = $"{Url.TrimEnd('/')}?ondate={date:yyyy-MM-dd}&curcode={quoteCurrency}";
				var json = await Http.GetStringAsync(url, ct);

				using var doc = JsonDocument.Parse(json);
				if (!doc.RootElement.TryGetProperty("Cur_OfficialRate", out var rateElement)) {
					continue;
				}

				if (!decimal.TryParse(rateElement.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) || rate <= 0) {
					continue;
				}

				rates.Add(new ExchangeRateResult(date, NativeCurrency, quoteCurrency, rate, Code));
			}
			catch {
				// Skip days with no data
				continue;
			}
		}

		return rates;
	}
}
