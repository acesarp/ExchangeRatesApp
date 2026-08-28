using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Thailand
/// </summary>
public sealed class BOTProvider : CentralBankProviderBase {
	public BOTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOT";
	public override string Name => "Bank of Thailand";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.THB;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
