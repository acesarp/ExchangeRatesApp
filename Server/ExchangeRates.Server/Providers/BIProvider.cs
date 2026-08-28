using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank Indonesia
/// </summary>
public sealed class BIProvider : CentralBankProviderBase {
	public BIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BI";
	public override string Name => "Bank Indonesia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.IDR;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
