using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Domain.Interfaces;

public interface IExchangeRateRepository {
	Task<IReadOnlyList<ExchangeRateEntity>> GetRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);

	Task AddRangeAsync(IEnumerable<ExchangeRateEntity> rates, CancellationToken ct);
	Task<IReadOnlyList<CurrencyEntity>> GetCurrenciesAsync(CancellationToken ct);
	Task<IEnumerable<CentralBankEntity>> GetCentralBanksAsync(CancellationToken ct);
}