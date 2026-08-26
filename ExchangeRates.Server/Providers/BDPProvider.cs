using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco de Portugal
/// </summary>
public sealed class BDPProvider : CentralBankProviderBase {
	public BDPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BDP";
	public override string Name => "Banco de Portugal";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.PTE;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
