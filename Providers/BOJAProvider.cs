using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Jamaica
/// </summary>
public sealed class BOJAProvider : CentralBankProviderBase {
	public BOJAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOJA";
	public override string Name => "Bank of Jamaica";
	public override ECurrency NativeCurrency => ECurrency.JMD;
}
