using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Cambodia
/// </summary>
public sealed class NBCProvider : CentralBankProviderBase {
	private readonly ILogger<NBCProvider> _logger;
	public NBCProvider(HttpClient http, IConfiguration configuration, ILogger<NBCProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBC";
	public override string Name => "National Bank of Cambodia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.KHR;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
