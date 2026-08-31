using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank Negara Malaysia
/// </summary>
public sealed class BNMProvider : CentralBankProviderBase {
	private readonly ILogger<BNMProvider> _logger;

	public BNMProvider(HttpClient http, IConfiguration configuration, ILogger<BNMProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BNM";
	public override string Name => "Bank Negara Malaysia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MYR;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
