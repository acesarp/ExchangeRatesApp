using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Kenya
/// </summary>
public sealed class CBKProvider : CentralBankProviderBase {
	public CBKProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBK";
	public override string Name => "Central Bank of Kenya";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.KES;
}
