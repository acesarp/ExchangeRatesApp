using ExchangeRates.Server.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque Centrale de Tunisie
/// </summary>
public sealed class BCTProvider : CentralBankProviderBase {
	public BCTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCT";
	public override string Name => "Banque Centrale de Tunisie";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.TND;

}
