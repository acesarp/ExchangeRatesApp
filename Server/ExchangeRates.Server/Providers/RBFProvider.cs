using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

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
	public override string Name => "Reserve Bank of Fiji";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.FJD;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
