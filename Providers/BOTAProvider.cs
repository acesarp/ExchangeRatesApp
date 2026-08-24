using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Tanzania
/// </summary>
public sealed class BOTAProvider : CentralBankProviderBase {
	public BOTAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOTA";
	public override string Name => "Bank of Tanzania";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.TZS;
}
