using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Liberia
/// </summary>
public sealed class CBLLRProvider : CentralBankProviderBase {
	private readonly ILogger<CBLLRProvider> _logger;

	public CBLLRProvider(HttpClient http, IConfiguration configuration, ILogger<CBLLRProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBLLR";
	public override string Name => "Central Bank of Liberia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.LRD;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
