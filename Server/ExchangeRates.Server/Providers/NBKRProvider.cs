using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of the Kyrgyz Republic
/// </summary>
public sealed class NBKRProvider : CentralBankProviderBase {
	private readonly ILogger<NBKRProvider> _logger;
	public NBKRProvider(HttpClient http, IConfiguration configuration, ILogger<NBKRProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBKR";
	public override string Name => "National Bank of the Kyrgyz Republic";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.KGS;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
