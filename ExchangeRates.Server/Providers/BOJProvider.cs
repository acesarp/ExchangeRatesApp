using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank of Japan
/// </summary>
public sealed class BOJProvider : CentralBankProviderBase {
	public BOJProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BOJ";
	public override string Name => "Bank of Japan";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.JPY;
}
