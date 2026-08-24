using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Monetary Authority of Singapore
/// </summary>
public sealed class MASProvider : CentralBankProviderBase {
	public MASProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "MAS";
	public override string Name => "Monetary Authority of Singapore";
	public override ECurrency NativeCurrency => ECurrency.SGD;
}
