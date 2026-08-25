using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Nigeria
/// </summary>
public sealed class CBNProvider : CentralBankProviderBase {
	public CBNProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBN";
	public override string Name => "Central Bank of Nigeria";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.NGN;
}
