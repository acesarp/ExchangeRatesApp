using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Bolivia
/// </summary>
public sealed class BCBOProvider : CentralBankProviderBase {
	private readonly ILogger<BCBOProvider> _logger;
	public BCBOProvider(HttpClient http, IConfiguration configuration, ILogger<BCBOProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCBO";
	public override string Name => "Banco Central de Bolivia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.BOB;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
