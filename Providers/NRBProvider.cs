using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Nepal Rastra Bank
/// </summary>
public sealed class NRBProvider : CentralBankProviderBase {
	public NRBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NRB";
	public override string Name => "Nepal Rastra Bank";
	public override ECurrency NativeCurrency => ECurrency.NPR;
}
