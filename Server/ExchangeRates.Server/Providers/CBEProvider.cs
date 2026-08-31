using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Egypt
/// </summary>
public sealed class CBEProvider : CentralBankProviderBase {
	private readonly ILogger<CBEProvider> _logger;

	public CBEProvider(HttpClient http, IConfiguration configuration, ILogger<CBEProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "CBE";
	public override string Name => "Central Bank of Egypt";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.EGP;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
