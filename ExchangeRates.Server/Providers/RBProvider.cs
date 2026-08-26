using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Sveriges Riksbank
/// </summary>
public sealed class RBProvider : CentralBankProviderBase {
	public RBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RB";
	public override string Name => "Sveriges Riksbank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.SEK;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
