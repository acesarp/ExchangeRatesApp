using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Vanuatu
/// </summary>
public sealed class RBVProvider : CentralBankProviderBase {
	public RBVProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBV";
	public override string Name => "Reserve Bank of Vanuatu";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.VUV;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
