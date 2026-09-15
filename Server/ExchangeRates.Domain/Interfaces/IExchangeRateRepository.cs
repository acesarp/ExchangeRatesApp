using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Domain.Interfaces;

public interface IExchangeRateRepository {
	Task<IReadOnlyList<ExchangeRateEntity>> GetRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);

	Task<int> AddRangeAsync(IEnumerable<ExchangeRateEntity> rates, CancellationToken ct);
	Task<IReadOnlyList<CurrencyEntity>> GetCurrenciesAsync(CancellationToken ct);
	Task<IEnumerable<CentralBankEntity>> GetCentralBanksAsync(CancellationToken ct);
	Task<CentralBankEntity> FindSuitableBankAsync(string currency1, string currency2, CancellationToken ct);
}