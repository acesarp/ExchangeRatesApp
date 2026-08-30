using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Magyar Nemzeti Bank
/// </summary>
public sealed class MNBProvider : CentralBankProviderBase {
	private readonly ILogger<MNBProvider> _logger;
	public MNBProvider(HttpClient http, IConfiguration configuration, ILogger<MNBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "MNB";
	public override string Name => "Magyar Nemzeti Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.HUF;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
