using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.ExchangeRates;

public sealed class FixedExchangeRates {
	private readonly IConfiguration _configuration;

	public FixedExchangeRates(IConfiguration configuration) {
		_configuration = configuration;
	}

	public bool TryGetRate(ECurrencyISO currency, out ECurrencyISO peggedOn, out decimal rate) {
		var section = _configuration.GetSection($"FixedExchangeRates:Rates:{currency}");

		if (!section.Exists()) {
			peggedOn = default;
			rate = default;
			return false;
		}

		if (!Enum.TryParse(section["PeggedOn"], true, out peggedOn) || !decimal.TryParse(section["Rate"], out rate)) {
			peggedOn = default;
			rate = default;
			return false;
		}

		return true;
	}
}