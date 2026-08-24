using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central del Paraguay
/// </summary>
public sealed class BCPProvider : CentralBankProviderBase {
	public BCPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCP";
	public override string Name => "Banco Central del Paraguay";
	public override ECurrency NativeCurrency => ECurrency.PYG;

}
