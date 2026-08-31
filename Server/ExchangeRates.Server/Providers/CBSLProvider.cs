using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Sri Lanka
/// </summary>
public sealed class CBSLProvider : CentralBankProviderBase {
	private readonly ILogger<CBSLProvider> _logger;

	public CBSLProvider(HttpClient http, IConfiguration configuration, ILogger<CBSLProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBSL";
	public override string Name => "Central Bank of Sri Lanka";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.LKR;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
