using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Vanuatu
/// </summary>
public sealed class RBVProvider : CentralBankProviderBase {
	private readonly ILogger<RBVProvider> _logger;

	public RBVProvider(HttpClient http, IConfiguration configuration, ILogger<RBVProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "RBV";
	public override string Name => "Reserve Bank of Vanuatu";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.VUV;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
