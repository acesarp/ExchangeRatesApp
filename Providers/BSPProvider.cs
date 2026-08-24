using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bangko Sentral ng Pilipinas
/// </summary>
public sealed class BSPProvider : CentralBankProviderBase {
	public BSPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BSP";
	public override string Name => "Bangko Sentral ng Pilipinas";
	public override ECurrency NativeCurrency => ECurrency.PHP;
}
