using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// State Bank of Pakistan
/// </summary>
public sealed class SBPProvider : CentralBankProviderBase {
	public SBPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "SBP";
	public override string Name => "State Bank of Pakistan";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.PKR;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
