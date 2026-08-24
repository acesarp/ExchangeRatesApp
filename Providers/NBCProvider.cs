using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Cambodia
/// </summary>
public sealed class NBCProvider : CentralBankProviderBase {
	public NBCProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBC";
	public override string Name => "National Bank of Cambodia";
	public override ECurrency NativeCurrency => ECurrency.KHR;
}
