using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banca Națională a României
/// </summary>
public sealed class BNRProvider : CentralBankProviderBase {
	private readonly ILogger<BNRProvider> _logger;

	public BNRProvider(HttpClient http, IConfiguration configuration, ILogger<BNRProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "BNR";
	public override string Name => "Banca Națională a României";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.RON;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
