using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Kazakhstan
/// </summary>
public sealed class NBKProvider : CentralBankProviderBase {
	public NBKProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBK";
	public override string Name => "National Bank of Kazakhstan";
	public override ECurrency NativeCurrency => ECurrency.KZT;
}
