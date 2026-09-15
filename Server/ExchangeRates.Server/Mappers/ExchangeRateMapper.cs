using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Mappers;

public static class ExchangeRateMapper {
	public static ExchangeRateEntity ToEntity(this ExchangeRateResult result, int baseCurrencyId, int quoteCurrencyId) {
		return new ExchangeRateEntity {
			Date = result.Date,
			BaseCurrencyId = baseCurrencyId,
			QuoteCurrencyId = quoteCurrencyId,
			Rate = result.Rate,
			Provider = result.Provider
		};
	}

	public static ExchangeRateResult ToResult(this ExchangeRateEntity entity) {
		return new ExchangeRateResult(entity.Date, entity.BaseCurrency.CurrencyCode, entity.QuoteCurrency.CurrencyCode, entity.Rate, entity.Provider);
	}
}