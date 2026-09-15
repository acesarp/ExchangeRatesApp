using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Mappers;

public static class ExchangeRateMapper {
	public static ExchangeRateEntity ToEntity(this ExchangeRateResult result) {
		return new ExchangeRateEntity {
			Date = result.Date,
			BaseCurrency = new CurrencyEntity(result.BaseCurrency, default, default, default, default),
			QuoteCurrency = new CurrencyEntity(result.QuoteCurrency, default, default, default, default),
			Rate = result.Rate,
			Provider = result.Provider
		};
	}

	public static ExchangeRateResult ToResult(this ExchangeRateEntity entity) {
		return new ExchangeRateResult(entity.Date, entity.BaseCurrency.CurrencyCode, entity.QuoteCurrency.CurrencyCode, entity.Rate, entity.Provider);
	}
}