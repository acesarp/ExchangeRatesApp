using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Malawi
/// </summary>
public sealed class RBMProvider : CentralBankProviderBase {
	public RBMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBM";
	public override string Name => "Reserve Bank of Malawi";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MWK;
}
