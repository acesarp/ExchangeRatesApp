using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Narodna Banka na Republika Severna Makedonija
/// </summary>
public sealed class NBRMProvider : CentralBankProviderBase {
	private readonly ILogger<NBRMProvider> _logger;

	public NBRMProvider(HttpClient http, IConfiguration configuration, ILogger<NBRMProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBRM";
	public override string Name => "Narodna Banka na Republika Severna Makedonija";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MKD;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
