using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Da Afghanistan Bank
/// </summary>
public sealed class DABProvider : CentralBankProviderBase {
	private readonly ILogger<DABProvider> _logger;
	public DABProvider(HttpClient http, IConfiguration configuration, ILogger<DABProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "DAB";
	public override string Name => "Da Afghanistan Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.AFN;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
