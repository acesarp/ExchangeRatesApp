using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Narodna Banka na Republika Severna Makedonija
/// </summary>
public sealed class NBRMProvider : CentralBankProviderBase {
	public NBRMProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBRM";
	public override string Name => "Narodna Banka na Republika Severna Makedonija";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.MKD;
}
