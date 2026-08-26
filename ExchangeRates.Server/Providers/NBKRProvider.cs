using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of the Kyrgyz Republic
/// </summary>
public sealed class NBKRProvider : CentralBankProviderBase {
	public NBKRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBKR";
	public override string Name => "National Bank of the Kyrgyz Republic";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.KGS;

	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
