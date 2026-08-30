using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Nepal Rastra Bank
/// </summary>
public sealed class NRBProvider : CentralBankProviderBase {
	private readonly ILogger<NRBProvider> _logger;
	public NRBProvider(HttpClient http, IConfiguration configuration, ILogger<NRBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NRB";
	public override string Name => "Nepal Rastra Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.NPR;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
