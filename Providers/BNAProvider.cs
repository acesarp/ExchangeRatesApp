using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Nacional de Angola
/// </summary>
public sealed class BNAProvider : CentralBankProviderBase {
	public BNAProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNA";
	public override string Name => "Banco Nacional de Angola";
	public override ECurrency NativeCurrency => ECurrency.AOA;

}
