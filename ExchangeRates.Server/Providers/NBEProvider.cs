using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Ethiopia
/// </summary>
public sealed class NBEProvider : CentralBankProviderBase {
	public NBEProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBE";
	public override string Name => "National Bank of Ethiopia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.ETB;
}
