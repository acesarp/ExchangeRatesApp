using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Samoa
/// </summary>
public sealed class CBSProvider : CentralBankProviderBase {
	private readonly ILogger<CBSProvider> _logger;
	public CBSProvider(HttpClient http, IConfiguration configuration, ILogger<CBSProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBS";
	public override string Name => "Central Bank of Samoa";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.WST;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
