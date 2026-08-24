using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Norges Bank
/// </summary>
public sealed class NBProvider : CentralBankProviderBase {
	public NBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NB";
	public override string Name => "Norges Bank";
	public override ECurrency NativeCurrency => ECurrency.NOK;
}
