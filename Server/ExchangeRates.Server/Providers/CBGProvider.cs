using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of The Gambia
/// </summary>
public sealed class CBGProvider : CentralBankProviderBase {
	public CBGProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBG";
	public override string Name => "Central Bank of The Gambia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.GMD;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
