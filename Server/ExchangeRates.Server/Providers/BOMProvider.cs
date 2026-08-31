using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Mongolia
/// </summary>
public sealed class BOMProvider : CentralBankProviderBase {
	private readonly ILogger<BOMProvider> _logger;

	public BOMProvider(HttpClient http, IConfiguration configuration, ILogger<BOMProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOM";
	public override string Name => "Bank of Mongolia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MNT;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
