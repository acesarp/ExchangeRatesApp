
using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Fiji
/// </summary>
public sealed class RBFProvider : CentralBankProviderBase {
	private readonly ILogger<RBFProvider> _logger;

	public RBFProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<RBFProvider> logger) : base(http, configuration, bank) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		_logger.LogWarning("RBF endpoint requires manual data discovery; parsing not supported.");
		return await Task.FromResult<IReadOnlyList<ExchangeRateResult>>(Array.Empty<ExchangeRateResult>());
	}
}

