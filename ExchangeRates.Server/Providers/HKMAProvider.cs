using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Hong Kong Monetary Authority
/// </summary>
public sealed class HKMAProvider : CentralBankProviderBase {
	public HKMAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "HKMA";
	public override string Name => "Hong Kong Monetary Authority";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.HKD;
}
