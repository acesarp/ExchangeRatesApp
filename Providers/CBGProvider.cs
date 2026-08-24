using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of The Gambia
/// </summary>
public sealed class CBGProvider : CentralBankProviderBase {
	public CBGProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBG";
	public override string Name => "Central Bank of The Gambia";
	public override ECurrency NativeCurrency => ECurrency.GMD;
}
