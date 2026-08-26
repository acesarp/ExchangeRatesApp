using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Central Bank of Iraq
/// </summary>
public sealed class CBIProvider : CentralBankProviderBase {
	public CBIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "CBI";
	public override string Name => "Central Bank of Iraq";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.IQD;
}
