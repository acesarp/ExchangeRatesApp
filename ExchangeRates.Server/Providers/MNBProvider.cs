using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Magyar Nemzeti Bank
/// </summary>
public sealed class MNBProvider : CentralBankProviderBase {
	public MNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "MNB";
	public override string Name => "Magyar Nemzeti Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.HUF;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
