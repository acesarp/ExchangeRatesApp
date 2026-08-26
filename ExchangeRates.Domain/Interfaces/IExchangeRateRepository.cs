using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Domain.Interfaces;

public interface IExchangeRateRepository {
	Task<IReadOnlyList<ExchangeRate>> GetAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);

	Task AddRangeAsync(IEnumerable<ExchangeRate> rates, CancellationToken ct);
}