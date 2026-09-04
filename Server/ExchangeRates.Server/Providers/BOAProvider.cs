using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Algeria
/// </summary>
public sealed class BOAProvider : CentralBankProviderBase {
	private readonly ILogger<BOAProvider> _logger;

	public BOAProvider(HttpClient http, IConfiguration configuration, ILogger<BOAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOA";

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		_logger.LogWarning("BOA has no configured Url; unable to fetch rates.");
		return Task.FromResult<IReadOnlyList<ExchangeRateResult>>([]);
	}
}
