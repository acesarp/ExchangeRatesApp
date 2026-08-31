using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Kenya
/// </summary>
public sealed class CBKProvider : CentralBankProviderBase {
	private readonly ILogger<CBKProvider> _logger;

	public CBKProvider(HttpClient http, IConfiguration configuration, ILogger<CBKProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBK";
	public override string Name => "Central Bank of Kenya";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.KES;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
