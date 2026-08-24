using ExchangeRates.Server.Enums;

namespace ExchangeRates.Server.Interfaces;

public interface IExchangeRateService {
	Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(ECurrency fromCurrency, ECurrency toCurrency, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default);
}