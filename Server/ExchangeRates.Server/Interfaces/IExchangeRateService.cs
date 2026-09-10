using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Models;
namespace ExchangeRates.Server.Interfaces;

public interface IExchangeRateService {
	Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(string quoteCurrency, string toCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
	Task<IReadOnlyList<CurrencyModel>> GetCurrenciesAsync(CancellationToken ct = default);
	Task<IReadOnlyList<CentralBankModel>> GetCentralBanksAsync(CancellationToken ct = default);
}