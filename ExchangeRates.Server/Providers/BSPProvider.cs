using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bangko Sentral ng Pilipinas
/// </summary>
public sealed class BSPProvider : CentralBankProviderBase {
	public BSPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BSP";
	public override string Name => "Bangko Sentral ng Pilipinas";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.PHP;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
