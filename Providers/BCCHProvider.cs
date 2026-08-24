using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Chile
/// </summary>
public sealed class BCCHProvider : CentralBankProviderBase {
	public BCCHProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCCH";
	public override string Name => "Banco Central de Chile";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.CLP;

}
