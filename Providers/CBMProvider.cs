using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Myanmar
/// </summary>
public sealed class CBMProvider : CentralBankProviderBase {
	public CBMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBM";
	public override string Name => "Central Bank of Myanmar";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MMK;
}
