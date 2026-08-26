using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Mappers;

public static class ExchangeRateMapper {
	public static ExchangeRateEntity ToEntity(this ExchangeRateResult result) {
		return new ExchangeRateEntity {
			Date = result.Date,
			BaseCurrency = result.BaseCurrency,
			QuoteCurrency = result.QuoteCurrency,
			Rate = result.Rate,
			Provider = result.Provider
		};
	}

	public static ExchangeRateResult ToResult(this ExchangeRateEntity entity) {
		return new ExchangeRateResult(entity.Date, entity.BaseCurrency, entity.QuoteCurrency, entity.Rate, entity.Provider);
	}
}