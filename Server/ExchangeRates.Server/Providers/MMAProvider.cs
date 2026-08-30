using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Maldives Monetary Authority
/// </summary>
public sealed class MMAProvider : CentralBankProviderBase {
	private readonly ILogger<MMAProvider> _logger;
	public MMAProvider(HttpClient http, IConfiguration configuration, ILogger<MMAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "MMA";
	public override string Name => "Maldives Monetary Authority";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MVR;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
