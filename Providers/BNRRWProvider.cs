using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banque Nationale du Rwanda
/// </summary>
public sealed class BNRRWProvider : CentralBankProviderBase {
	public BNRRWProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BNRRW";
	public override string Name => "Banque Nationale du Rwanda";
	public override ECurrency NativeCurrency => ECurrency.RWF;

}
