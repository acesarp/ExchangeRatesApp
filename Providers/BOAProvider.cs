using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Algeria
/// </summary>
public sealed class BOAProvider : CentralBankProviderBase {
	public BOAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOA";
	public override string Name => "Bank of Algeria";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.DZD;

}
