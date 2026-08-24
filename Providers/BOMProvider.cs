using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Mongolia
/// </summary>
public sealed class BOMProvider : CentralBankProviderBase {
	public BOMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOM";
	public override string Name => "Bank of Mongolia";
	public override ECurrency NativeCurrency => ECurrency.MNT;
}
