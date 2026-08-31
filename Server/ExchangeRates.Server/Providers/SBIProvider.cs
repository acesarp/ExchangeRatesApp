using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Seðlabanki Íslands
/// </summary>
public sealed class SBIProvider : CentralBankProviderBase {
	private readonly ILogger<SBIProvider> _logger;

	public SBIProvider(HttpClient http, IConfiguration configuration, ILogger<SBIProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "SBI";
	public override string Name => "Seðlabanki Íslands";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.ISK;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
