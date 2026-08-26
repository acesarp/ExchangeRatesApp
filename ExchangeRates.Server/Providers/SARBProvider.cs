using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// South African Reserve Bank
/// </summary>
public sealed class SARBProvider : CentralBankProviderBase {
	public SARBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "SARB";
	public override string Name => "South African Reserve Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.ZAR;
}
