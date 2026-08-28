using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Natsyyanalny Bank Respubliki Belarus
/// </summary>
public sealed class NBRBProvider : CentralBankProviderBase {
	public NBRBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBRB";
	public override string Name => "Natsyyanalny Bank Respubliki Belarus";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.BYN;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
