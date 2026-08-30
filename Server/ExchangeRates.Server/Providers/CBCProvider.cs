using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of the Republic of China (Taiwan)
/// </summary>
public sealed class CBCProvider : CentralBankProviderBase {
	private readonly ILogger<CBCProvider> _logger;
	public CBCProvider(HttpClient http, IConfiguration configuration, ILogger<CBCProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBC";
	public override string Name => "Central Bank of the Republic of China (Taiwan)";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.TWD;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
