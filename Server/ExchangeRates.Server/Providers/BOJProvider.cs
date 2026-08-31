using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Japan
/// </summary>
public sealed class BOJProvider : CentralBankProviderBase {
	private readonly ILogger<BOJProvider> _logger;

	public BOJProvider(HttpClient http, IConfiguration configuration, ILogger<BOJProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOJ";
	public override string Name => "Bank of Japan";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.JPY;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
