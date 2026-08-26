using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Chile
/// </summary>
public sealed class BCCHProvider : CentralBankProviderBase {
	public BCCHProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCCH";
	public override string Name => "Banco Central de Chile";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.CLP;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
