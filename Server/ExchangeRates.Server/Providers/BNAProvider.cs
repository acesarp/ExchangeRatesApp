using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Nacional de Angola
/// </summary>
public sealed class BNAProvider : CentralBankProviderBase {
	private readonly ILogger<BNAProvider> _logger;

	public BNAProvider(HttpClient http, IConfiguration configuration, ILogger<BNAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BNA";
	public override string Name => "Banco Nacional de Angola";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.AOA;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
