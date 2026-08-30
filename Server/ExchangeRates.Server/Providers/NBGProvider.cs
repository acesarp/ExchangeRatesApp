using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Georgia
/// </summary>
public sealed class NBGProvider : CentralBankProviderBase {
	private readonly ILogger<NBGProvider> _logger;
	public NBGProvider(HttpClient http, IConfiguration configuration, ILogger<NBGProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBG";
	public override string Name => "National Bank of Georgia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.GEL;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
