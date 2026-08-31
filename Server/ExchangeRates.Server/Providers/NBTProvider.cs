using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Tajikistan
/// </summary>
public sealed class NBTProvider : CentralBankProviderBase {
	private readonly ILogger<NBTProvider> _logger;

	public NBTProvider(HttpClient http, IConfiguration configuration, ILogger<NBTProvider> logger) : base(http, configuration) {
		_logger = logger;
	}

	public override string Code => "NBT";
	public override string Name => "National Bank of Tajikistan";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.TJS;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
