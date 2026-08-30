using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Myanmar
/// </summary>
public sealed class CBMProvider : CentralBankProviderBase {
	private readonly ILogger<CBMProvider> _logger;
	public CBMProvider(HttpClient http, IConfiguration configuration, ILogger<CBMProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBM";
	public override string Name => "Central Bank of Myanmar";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MMK;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
