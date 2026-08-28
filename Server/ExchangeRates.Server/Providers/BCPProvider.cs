using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central del Paraguay
/// </summary>
public sealed class BCPProvider : CentralBankProviderBase {
	public BCPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCP";
	public override string Name => "Banco Central del Paraguay";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.PYG;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
