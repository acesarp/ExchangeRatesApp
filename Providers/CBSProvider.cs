using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Samoa
/// </summary>
public sealed class CBSProvider : CentralBankProviderBase {
	public CBSProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBS";
	public override string Name => "Central Bank of Samoa";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.WST;
}
