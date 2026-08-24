using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Moldova
/// </summary>
public sealed class NBMProvider : CentralBankProviderBase {
	public NBMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBM";
	public override string Name => "National Bank of Moldova";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MDL;
}
