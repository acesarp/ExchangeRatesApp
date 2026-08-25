using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Liberia
/// </summary>
public sealed class CBLLRProvider : CentralBankProviderBase {
	public CBLLRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBLLR";
	public override string Name => "Central Bank of Liberia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.LRD;
}
