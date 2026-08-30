using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Ethiopia
/// </summary>
public sealed class NBEProvider : CentralBankProviderBase {
	private readonly ILogger<NBEProvider> _logger;
	public NBEProvider(HttpClient http, IConfiguration configuration, ILogger<NBEProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBE";
	public override string Name => "National Bank of Ethiopia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.ETB;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
