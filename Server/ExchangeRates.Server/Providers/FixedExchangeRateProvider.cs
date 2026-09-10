namespace ExchangeRates.Server.Providers;

public sealed class FixedExchangeRateProvider {
	private readonly IReadOnlyDictionary<string, FixedExchangeRate> _rates;

	public FixedExchangeRateProvider(IConfiguration configuration) {
		_rates = configuration.GetSection("FixedExchangeRates:Rates")
											.GetChildren()
											.ToDictionary(x => x.Key, x => new FixedExchangeRate(
												x.Key,
												x["PeggedOn"]!,
												decimal.Parse(x["Rate"]!, System.Globalization.CultureInfo.InvariantCulture)));
	}

	public bool TryGetFixedRate(string currency, out string peggedOn, out decimal rate) {
		if (_rates.TryGetValue(currency, out var fixedRate)) {
			peggedOn = fixedRate.PeggedOn;
			rate = fixedRate.Rate;
			return true;
		}

		peggedOn = default;
		rate = default;
		return false;
	}

	private sealed record FixedExchangeRate(string BaseCurrency, string PeggedOn, decimal Rate);
}