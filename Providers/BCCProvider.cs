using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Cuba
/// </summary>
public sealed class BCCProvider : CentralBankProviderBase {
	public BCCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCC";
	public override string Name => "Banco Central de Cuba";
	public override ECurrency NativeCurrency => ECurrency.CUP;

}
