using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Da Afghanistan Bank
/// </summary>
public sealed class DABProvider : CentralBankProviderBase {
	public DABProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "DAB";
	public override string Name => "Da Afghanistan Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.AFN;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
