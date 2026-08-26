using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Georgia
/// </summary>
public sealed class NBGProvider : CentralBankProviderBase {
	public NBGProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBG";
	public override string Name => "National Bank of Georgia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.GEL;
}
