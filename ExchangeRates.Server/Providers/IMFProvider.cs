using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// International Monetary Fund
/// </summary>
public sealed class IMFProvider : CentralBankProviderBase {
	public IMFProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "IMF";
	public override string Name => "International Monetary Fund";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.XDR;
}
