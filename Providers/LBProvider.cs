using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Lietuvos Bankas
/// </summary>
public sealed class LBProvider : CentralBankProviderBase {
	public LBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "LB";
	public override string Name => "Lietuvos Bankas";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.EUR;
}
