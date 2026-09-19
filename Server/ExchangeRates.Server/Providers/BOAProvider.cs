
using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Algeria
/// </summary>
public sealed class BOAProvider : CentralBankProviderBase {
	private readonly ILogger<BOAProvider> _logger;

	public BOAProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BOAProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		_logger.LogWarning("BOA has no configured ApiUrl; unable to fetch rates.");
		return Task.FromResult<IReadOnlyList<ExchangeRateResult>>([]);
	}
}

