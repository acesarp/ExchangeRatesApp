using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Banco de Portugal
/// </summary>
public sealed class BDPProvider : CentralBankProviderBase {
	public BDPProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BDP";
	public override string Name => "Banco de Portugal";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.PTE;

}
