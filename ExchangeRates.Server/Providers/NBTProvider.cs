using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// National Bank of Tajikistan
/// </summary>
public sealed class NBTProvider : CentralBankProviderBase {
	public NBTProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "NBT";
	public override string Name => "National Bank of Tajikistan";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.TJS;
}
