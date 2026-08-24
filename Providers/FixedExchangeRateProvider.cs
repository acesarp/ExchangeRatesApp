using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Extensions;

using System.Globalization;

namespace ExchangeRates.Server.Providers;

public sealed class FixedExchangeRateProvider {
	private readonly Dictionary<ECurrencyISO, decimal> _rates;

	public FixedExchangeRateProvider(IConfiguration configuration) {
		_rates = configuration.GetSection("FixedExchangeRates:Rates")
											.GetChildren()
											.ToDictionary(x => x.Key.ToECurrency(),
																x => decimal.Parse(x.Value!, CultureInfo.InvariantCulture));
	}

	public bool TryGetFixedRate(ECurrencyISO currency, out decimal rate) {
		return _rates.TryGetValue(currency, out rate);
	}
}
