using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Reserve Bank of Tonga
/// </summary>
public sealed class NRBTProvider : CentralBankProviderBase {
	private readonly ILogger<NRBTProvider> _logger;

	public NRBTProvider(HttpClient http, IConfiguration configuration, ILogger<NRBTProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NRBT";
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		_logger.LogWarning("NRBT source is an XLSX file; parsing not supported.");
		return await Task.FromResult<IReadOnlyList<ExchangeRateResult>>(Array.Empty<ExchangeRateResult>());
	}
}
