using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Hrvatska Narodna Banka
/// </summary>
public sealed class HNBProvider : CentralBankProviderBase {
	private readonly ILogger<HNBProvider> _logger;

	public HNBProvider(HttpClient http, IConfiguration configuration, ILogger<HNBProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "HNB";
	public override string Name => "Hrvatska Narodna Banka";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.EUR;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
