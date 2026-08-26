using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Sri Lanka
/// </summary>
public sealed class CBSLProvider : CentralBankProviderBase {
	public CBSLProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBSL";
	public override string Name => "Central Bank of Sri Lanka";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.LKR;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
