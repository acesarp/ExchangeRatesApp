using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Fiji
/// </summary>
public sealed class RBFProvider : CentralBankProviderBase {
	private readonly ILogger<RBFProvider> _logger;

	public RBFProvider(HttpClient http, IConfiguration configuration, ILogger<RBFProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "RBF";
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		_logger.LogWarning("RBF endpoint requires manual data discovery; parsing not supported.");
		return await Task.FromResult<IReadOnlyList<ExchangeRateResult>>(Array.Empty<ExchangeRateResult>());
	}
}
