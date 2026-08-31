using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Botswana
/// </summary>
public sealed class BOBProvider : CentralBankProviderBase {
	private readonly ILogger<BOBProvider> _logger;

	public BOBProvider(HttpClient http, IConfiguration configuration, ILogger<BOBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BOB";
	public override string Name => "Bank of Botswana";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.BWP;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
