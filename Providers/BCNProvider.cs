using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Nicaragua
/// </summary>
public sealed class BCNProvider : CentralBankProviderBase {
	public BCNProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCN";
	public override string Name => "Banco Central de Nicaragua";
	public override ECurrency NativeCurrency => ECurrency.NIO;

}
