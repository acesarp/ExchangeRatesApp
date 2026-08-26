using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Interfaces;

public interface IExchangeRateService {
	Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(ECurrencyISO quoteCurrency, ECurrencyISO toCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
}