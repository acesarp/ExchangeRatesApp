using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Danmarks Nationalbank
/// </summary>
public sealed class DNBProvider : CentralBankProviderBase {
	public DNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "DNB";
	public override string Name => "Danmarks Nationalbank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.DKK;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
