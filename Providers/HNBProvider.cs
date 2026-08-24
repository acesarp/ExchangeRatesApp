using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Hrvatska Narodna Banka
/// </summary>
public sealed class HNBProvider : CentralBankProviderBase {
	public HNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "HNB";
	public override string Name => "Hrvatska Narodna Banka";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.EUR;
}
