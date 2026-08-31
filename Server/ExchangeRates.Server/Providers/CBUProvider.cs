using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Uzbekistan
/// </summary>
public sealed class CBUProvider : CentralBankProviderBase {
	private readonly ILogger<CBUProvider> _logger;

	public CBUProvider(HttpClient http, IConfiguration configuration, ILogger<CBUProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBU";
	public override string Name => "Central Bank of Uzbekistan";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.UZS;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
