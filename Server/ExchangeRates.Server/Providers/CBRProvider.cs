using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Russia
/// </summary>
public sealed class CBRProvider : CentralBankProviderBase {
	private readonly ILogger<CBRProvider> _logger;
	public CBRProvider(HttpClient http, IConfiguration configuration, ILogger<CBRProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBR";
	public override string Name => "Central Bank of Russia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.RUB;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
