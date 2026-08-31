using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Reserve Bank of Tonga
/// </summary>
public sealed class NRBTProvider : CentralBankProviderBase {
	private readonly ILogger<NRBTProvider> _logger;

	public NRBTProvider(HttpClient http, IConfiguration configuration, ILogger<NRBTProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NRBT";
	public override string Name => "National Reserve Bank of Tonga";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.TOP;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
