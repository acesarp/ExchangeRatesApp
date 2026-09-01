using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// State Bank of Pakistan
/// </summary>
public sealed class SBPProvider : CentralBankProviderBase {
	private readonly ILogger<SBPProvider> _logger;
	public SBPProvider(HttpClient http, IConfiguration configuration, ILogger<SBPProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "SBP";
	public override string Name => "State Bank of Pakistan";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.PKR;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		_logger.LogWarning("SBP source is an XLSX file; parsing not supported.");
		return Task.FromResult<IReadOnlyList<ExchangeRateResult>>([]);
	}
}
