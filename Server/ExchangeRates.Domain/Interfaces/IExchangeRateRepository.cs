using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Domain.Interfaces;

public interface IExchangeRateRepository {
	Task<IReadOnlyList<ExchangeRateEntity>> GetAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, bool inverseRate, CancellationToken ct);

	Task AddRangeAsync(IEnumerable<ExchangeRateEntity> rates, CancellationToken ct);

}