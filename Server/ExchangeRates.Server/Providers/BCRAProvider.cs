using ExchangeRates.Domain.Enums;


namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de la República Argentina
/// </summary>
public sealed class BCRAProvider : CentralBankProviderBase {
	private readonly ILogger<BCRAProvider> _logger;

	public BCRAProvider(HttpClient http, IConfiguration configuration, ILogger<BCRAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCRA";
	public override string Name => "Banco Central de la República Argentina";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.ARS;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
