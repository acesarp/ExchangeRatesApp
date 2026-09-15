
using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// State Bank of Pakistan
/// </summary>
public sealed class SBPProvider : CentralBankProviderBase {
	private readonly ILogger<SBPProvider> _logger;
	public SBPProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<SBPProvider> logger) : base(http, configuration) {
		_logger = logger;
	}
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		_logger.LogWarning("SBP source is an XLSX file; parsing not supported.");
		return Task.FromResult<IReadOnlyList<ExchangeRateResult>>([]);
	}
}

