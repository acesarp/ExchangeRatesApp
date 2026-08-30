using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Kazakhstan
/// </summary>
public sealed class NBKProvider : CentralBankProviderBase {
	private readonly ILogger<NBKProvider> _logger;
	public NBKProvider(HttpClient http, IConfiguration configuration, ILogger<NBKProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBK";
	public override string Name => "National Bank of Kazakhstan";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.KZT;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
