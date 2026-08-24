using ExchangeRates.Server.Enums;
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
	public BNRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNR";
	public override string Name => "Banca Națională a României";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.RON;

}
