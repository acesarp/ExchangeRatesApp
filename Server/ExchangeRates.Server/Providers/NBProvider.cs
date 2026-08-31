using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Norges Bank
/// </summary>
public sealed class NBProvider : CentralBankProviderBase {
	private readonly ILogger<NBProvider> _logger;

	public NBProvider(HttpClient http, IConfiguration configuration, ILogger<NBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NB";
	public override string Name => "Norges Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.NOK;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
