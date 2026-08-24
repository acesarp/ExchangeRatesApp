using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central de Bolivia
/// </summary>
public sealed class BCBOProvider : CentralBankProviderBase {
	public BCBOProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCBO";
	public override string Name => "Banco Central de Bolivia";
	public override ECurrency NativeCurrency => ECurrency.BOB;

}
