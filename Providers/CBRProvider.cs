using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Russia
/// </summary>
public sealed class CBRProvider : CentralBankProviderBase {
	public CBRProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBR";
	public override string Name => "Central Bank of Russia";
	public override ECurrency NativeCurrency => ECurrency.RUB;
}
