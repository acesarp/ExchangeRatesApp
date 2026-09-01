using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

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
	public override string Name => "Bank of Algeria";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.DZD;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		_logger.LogWarning("BOA has no configured Url; unable to fetch rates.");
		return Task.FromResult<IReadOnlyList<ExchangeRateResult>>([]);
	}
}
