using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Financial Benchmarks India
/// </summary>
public sealed class FBILProvider : CentralBankProviderBase {
	public FBILProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "FBIL";
	public override string Name => "Financial Benchmarks India";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.INR;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
