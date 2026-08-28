using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Czech National Bank
/// </summary>
public sealed class CNBProvider : CentralBankProviderBase {
	public CNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CNB";
	public override string Name => "Czech National Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.CZK;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
