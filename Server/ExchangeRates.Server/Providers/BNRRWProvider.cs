using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque Nationale du Rwanda
/// </summary>
public sealed class BNRRWProvider : CentralBankProviderBase {
	private readonly ILogger<BNRRWProvider> _logger;

	public BNRRWProvider(HttpClient http, IConfiguration configuration, ILogger<BNRRWProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BNRRW";
	public override string Name => "Banque Nationale du Rwanda";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.RWF;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
