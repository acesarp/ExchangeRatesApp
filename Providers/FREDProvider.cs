using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Federal Reserve Bank of St. Louis
/// </summary>
public sealed class FREDProvider : CentralBankProviderBase {
	public FREDProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "FRED";
	public override string Name => "Federal Reserve Bank of St. Louis";
	public override ECurrency NativeCurrency => ECurrency.USD;
}
