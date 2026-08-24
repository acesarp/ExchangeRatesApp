using ExchangeRates.Server.Enums;

namespace ExchangeRates.Server.Interfaces;

public interface IExchangeRateService {
	Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(ECurrencyISO fromCurrency, ECurrencyISO toCurrency, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default);
}