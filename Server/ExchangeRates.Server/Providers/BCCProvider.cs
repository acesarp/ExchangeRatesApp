using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Cuba
/// </summary>
public sealed class BCCProvider : CentralBankProviderBase {
	private readonly ILogger<BCCProvider> _logger;
	public BCCProvider(HttpClient http, IConfiguration configuration, ILogger<BCCProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCC";
	public override string Name => "Banco Central de Cuba";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.CUP;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
