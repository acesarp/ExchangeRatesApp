using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque Centrale de Tunisie
/// </summary>
public sealed class BCTProvider : CentralBankProviderBase {
	private readonly ILogger<BCTProvider> _logger;

	public BCTProvider(HttpClient http, IConfiguration configuration, ILogger<BCTProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCT";
	public override string Name => "Banque Centrale de Tunisie";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.TND;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
