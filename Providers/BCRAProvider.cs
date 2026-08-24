using ExchangeRates.Server.Enums;


namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de la República Argentina
/// </summary>
public sealed class BCRAProvider : CentralBankProviderBase {
	public BCRAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCRA";
	public override string Name => "Banco Central de la República Argentina";
	public override ECurrency NativeCurrency => ECurrency.ARS;

}
