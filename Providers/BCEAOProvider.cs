using ExchangeRates.Server.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque Centrale des Etats de l'Afrique de l'Ouest
/// </summary>
public sealed class BCEAOProvider : CentralBankProviderBase {
	public BCEAOProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCEAO";
	public override string Name => "Banque Centrale des Etats de l'Afrique de l'Ouest";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.XOF;

}
