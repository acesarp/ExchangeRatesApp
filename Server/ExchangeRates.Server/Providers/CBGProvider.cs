using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of The Gambia
/// </summary>
public sealed class CBGProvider : CentralBankProviderBase {
	private readonly ILogger<CBGProvider> _logger;
	public CBGProvider(HttpClient http, IConfiguration configuration, ILogger<CBGProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBG";
	public override string Name => "Central Bank of The Gambia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.GMD;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
