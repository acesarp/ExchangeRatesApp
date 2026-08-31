using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Jamaica
/// </summary>
public sealed class BOJAProvider : CentralBankProviderBase {
	private readonly ILogger<BOJAProvider> _logger;

	public BOJAProvider(HttpClient http, IConfiguration configuration, ILogger<BOJAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOJA";
	public override string Name => "Bank of Jamaica";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.JMD;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
