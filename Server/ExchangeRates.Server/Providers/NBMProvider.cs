using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Moldova
/// </summary>
public sealed class NBMProvider : CentralBankProviderBase {
	private readonly ILogger<NBMProvider> _logger;
	public NBMProvider(HttpClient http, IConfiguration configuration, ILogger<NBMProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBM";
	public override string Name => "National Bank of Moldova";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MDL;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
