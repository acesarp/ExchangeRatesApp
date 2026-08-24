using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Fiji
/// </summary>
public sealed class RBFProvider : CentralBankProviderBase {
	public RBFProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBF";
	public override string Name => "Reserve Bank of Fiji";
	public override ECurrency NativeCurrency => ECurrency.FJD;
}
