using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bangko Sentral ng Pilipinas
/// </summary>
public sealed class BSPProvider : CentralBankProviderBase {
	private readonly ILogger<BSPProvider> _logger;

	public BSPProvider(HttpClient http, IConfiguration configuration, ILogger<BSPProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BSP";
	public override string Name => "Bangko Sentral ng Pilipinas";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.PHP;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
