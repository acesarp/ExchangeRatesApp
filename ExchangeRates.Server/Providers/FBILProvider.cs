using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Financial Benchmarks India
/// </summary>
public sealed class FBILProvider : CentralBankProviderBase {
	public FBILProvider(HttpClient http, IConfiguration configuration) : base(http, configuration) { }

	public override string Code => "FBIL";
	public override string Name => "Financial Benchmarks India";
	public override ECurrencyISO NativeCurrency => ECurrencyISO.INR;
}
