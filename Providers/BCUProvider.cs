using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco Central del Uruguay
/// </summary>
public sealed class BCUProvider : CentralBankProviderBase {
	public BCUProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BCU";
	public override string Name => "Banco Central del Uruguay";
	public override ECurrency NativeCurrency => ECurrency.UYU;

}
