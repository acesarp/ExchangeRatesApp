using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Nigeria
/// </summary>
public sealed class CBNProvider : CentralBankProviderBase {
	private readonly ILogger<CBNProvider> _logger;

	public CBNProvider(HttpClient http, IConfiguration configuration, ILogger<CBNProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBN";
	public override string Name => "Central Bank of Nigeria";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.NGN;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
