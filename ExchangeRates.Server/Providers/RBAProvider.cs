using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Reserve Bank of Australia
/// </summary>
public sealed class RBAProvider : CentralBankProviderBase {
	public RBAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "RBA";
	public override string Name => "Reserve Bank of Australia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.AUD;

}
