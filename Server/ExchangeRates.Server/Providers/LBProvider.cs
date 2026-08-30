using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Lietuvos Bankas
/// </summary>
public sealed class LBProvider : CentralBankProviderBase {
	private readonly ILogger<LBProvider> _logger;
	public LBProvider(HttpClient http, IConfiguration configuration, ILogger<LBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "LB";
	public override string Name => "Lietuvos Bankas";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.EUR;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
