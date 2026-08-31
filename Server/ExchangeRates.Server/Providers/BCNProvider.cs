using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Nicaragua
/// </summary>
public sealed class BCNProvider : CentralBankProviderBase {
	private readonly ILogger<BCNProvider> _logger;

	public BCNProvider(HttpClient http, IConfiguration configuration, ILogger<BCNProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCN";
	public override string Name => "Banco Central de Nicaragua";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.NIO;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
