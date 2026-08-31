using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Tanzania
/// </summary>
public sealed class BOTAProvider : CentralBankProviderBase {
	private readonly ILogger<BOTAProvider> _logger;

	public BOTAProvider(HttpClient http, IConfiguration configuration, ILogger<BOTAProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOTA";
	public override string Name => "Bank of Tanzania";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.TZS;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
