using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Hong Kong Monetary Authority
/// </summary>
public sealed class HKMAProvider : CentralBankProviderBase {
	public HKMAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "HKMA";
	public override string Name => "Hong Kong Monetary Authority";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.HKD;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
