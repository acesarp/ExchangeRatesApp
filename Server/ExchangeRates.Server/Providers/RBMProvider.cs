using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Malawi
/// </summary>
public sealed class RBMProvider : CentralBankProviderBase {
	private readonly ILogger<RBMProvider> _logger;
	public RBMProvider(HttpClient http, IConfiguration configuration, ILogger<RBMProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "RBM";
	public override string Name => "Reserve Bank of Malawi";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MWK;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
