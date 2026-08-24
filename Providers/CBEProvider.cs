using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Egypt
/// </summary>
public sealed class CBEProvider : CentralBankProviderBase {
	public CBEProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBE";
	public override string Name => "Central Bank of Egypt";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.EGP;
}
