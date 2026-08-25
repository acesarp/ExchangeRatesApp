using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Bank Indonesia
/// </summary>
public sealed class BIProvider : CentralBankProviderBase {
	public BIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "BI";
	public override string Name => "Bank Indonesia";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.IDR;

}
