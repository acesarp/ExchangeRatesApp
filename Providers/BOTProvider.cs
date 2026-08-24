using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Thailand
/// </summary>
public sealed class BOTProvider : CentralBankProviderBase {
	public BOTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOT";
	public override string Name => "Bank of Thailand";
	public override ECurrency NativeCurrency => ECurrency.THB;
}
