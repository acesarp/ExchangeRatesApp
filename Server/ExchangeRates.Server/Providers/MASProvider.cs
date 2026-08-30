using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Monetary Authority of Singapore
/// </summary>
public sealed class MASProvider : CentralBankProviderBase {
	private readonly ILogger<MASProvider> _logger;
	public MASProvider(HttpClient http, IConfiguration configuration, ILogger<MASProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "MAS";
	public override string Name => "Monetary Authority of Singapore";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.SGD;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
