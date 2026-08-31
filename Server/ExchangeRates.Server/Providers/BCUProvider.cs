using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central del Uruguay
/// </summary>
public sealed class BCUProvider : CentralBankProviderBase {
	private readonly ILogger<BCUProvider> _logger;

	public BCUProvider(HttpClient http, IConfiguration configuration, ILogger<BCUProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BCU";
	public override string Name => "Banco Central del Uruguay";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.UYU;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
