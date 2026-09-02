using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Extensions;

namespace ExchangeRates.Server.Providers;

public sealed class FixedExchangeRateProvider {
	private readonly IReadOnlyDictionary<ECurrencyISO, FixedExchangeRate> _rates;

	public FixedExchangeRateProvider(IConfiguration configuration) {
		_rates = configuration.GetSection("FixedExchangeRates:Rates")
											.GetChildren()
											.ToDictionary(x => x.Key.ToECurrency(), x => new FixedExchangeRate(
												x.Key.ToECurrency(),
												x["PeggedOn"]!.ToECurrency(),
												decimal.Parse(x["Rate"]!, System.Globalization.CultureInfo.InvariantCulture)));
	}

	public bool TryGetFixedRate(ECurrencyISO currency, out ECurrencyISO peggedOn, out decimal rate) {
		if (_rates.TryGetValue(currency, out var fixedRate)) {
			peggedOn = fixedRate.PeggedOn;
			rate = fixedRate.Rate;
			return true;
		}

		peggedOn = default;
		rate = default;
		return false;
	}

	private sealed record FixedExchangeRate(ECurrencyISO BaseCurrency, ECurrencyISO PeggedOn, decimal Rate);
}