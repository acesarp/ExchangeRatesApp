using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Domain.Interfaces;

public interface IExchangeRateRepository {
	Task<IReadOnlyList<ExchangeRateEntity>> GetAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);

	Task AddRangeAsync(IEnumerable<ExchangeRateEntity> rates, CancellationToken ct);
	Task<IReadOnlyList<ExchangeRateFetch>> GetFetchesAsync(string provider, ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);

	Task AddFetchAsync(ExchangeRateFetch fetch, CancellationToken ct);
}