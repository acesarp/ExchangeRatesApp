using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Czech National Bank
/// </summary>
public sealed class CNBProvider : CentralBankProviderBase {
	private readonly ILogger<CNBProvider> _logger;

	public CNBProvider(HttpClient http, IConfiguration configuration, ILogger<CNBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CNB";
	public override string Name => "Czech National Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.CZK;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
