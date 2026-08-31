using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque Centrale des Etats de l'Afrique de l'Ouest
/// </summary>
public sealed class BCEAOProvider : CentralBankProviderBase {
	private readonly ILogger<BCEAOProvider> _logger;

	public BCEAOProvider(HttpClient http, IConfiguration configuration, ILogger<BCEAOProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCEAO";
	public override string Name => "Banque Centrale des Etats de l'Afrique de l'Ouest";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.XOF;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
