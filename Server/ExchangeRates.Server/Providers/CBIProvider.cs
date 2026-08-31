using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Iraq
/// </summary>
public sealed class CBIProvider : CentralBankProviderBase {
	private readonly ILogger<CBIProvider> _logger;

	public CBIProvider(HttpClient http, IConfiguration configuration, ILogger<CBIProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBI";
	public override string Name => "Central Bank of Iraq";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.IQD;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
