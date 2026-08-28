using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque de la Republique du Burundi
/// </summary>
public sealed class BRBProvider : CentralBankProviderBase {
	public BRBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BRB";
	public override string Name => "Banque de la Republique du Burundi";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.BIF;
	protected override Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
