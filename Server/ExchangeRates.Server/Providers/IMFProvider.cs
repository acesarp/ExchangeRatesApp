using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// International Monetary Fund
/// </summary>
public sealed class IMFProvider : CentralBankProviderBase {
	private readonly ILogger<IMFProvider> _logger;
	public IMFProvider(HttpClient http, IConfiguration configuration, ILogger<IMFProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "IMF";
	public override string Name => "International Monetary Fund";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.XDR;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
