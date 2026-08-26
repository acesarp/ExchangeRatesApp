using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Seðlabanki Íslands
/// </summary>
public sealed class SBIProvider : CentralBankProviderBase {
	public SBIProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "SBI";
	public override string Name => "Seðlabanki Íslands";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.ISK;
}
