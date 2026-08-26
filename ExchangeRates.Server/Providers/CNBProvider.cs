using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Czech National Bank
/// </summary>
public sealed class CNBProvider : CentralBankProviderBase {
	public CNBProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CNB";
	public override string Name => "Czech National Bank";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.CZK;
}
