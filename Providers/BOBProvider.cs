using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Botswana
/// </summary>
public sealed class BOBProvider : CentralBankProviderBase {
	public BOBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOB";
	public override string Name => "Bank of Botswana";
	public override ECurrency NativeCurrency => ECurrency.BWP;
}
