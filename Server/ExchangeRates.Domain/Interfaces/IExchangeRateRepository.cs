using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Domain.Interfaces;

public interface IExchangeRateRepository {

	Task<IReadOnlyList<ExchangeRateEntity>> GetRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);

	Task<int> AddRangeAsync(IEnumerable<ExchangeRateEntity> rates, CancellationToken ct);
	Task<IReadOnlyList<CurrencyEntity>> GetCurrenciesAsync(CancellationToken ct);
	Task<IReadOnlyList<CurrencyEntity>> GetCurrencyByCodesAsync(IEnumerable<string> currencyCodes, CancellationToken ct);
	Task<IEnumerable<CentralBankEntity>> GetCentralBanksAsync(CancellationToken ct);
	Task<CentralBankEntity> GetCentralBankAsync(string bankCode, CancellationToken ct);
	/// <summary>
	/// Gets the active central banks that publish both currencies, either as their native currency or as a supported one,
	/// ordered by priority. Banks whose native currency is one of the two can quote the pair directly; the others can
	/// only provide a cross rate through their native currency.
	/// </summary>
	Task<IReadOnlyList<CentralBankEntity>> GetBanksPublishingAsync(string currency1, string currency2, CancellationToken ct);
	Task<int> AddUnavailableDatesAsync(IEnumerable<ExchangeRateUnavailableDateEntity> unavailableDates, CancellationToken ct);
	Task<HashSet<ExchangeRateUnavailableDateEntity>> GetUnavailableDateEntitiesAsync(DateOnly from, DateOnly to, CancellationToken ct);
	/// <summary> Gets the active fixed pegs of a currency: 1 unit of PeggedOnCurrency = Rate units of the currency </summary>
	Task<IReadOnlyList<FixedExchangeRateEntity>> GetFixedRatesAsync(string currencyCode, CancellationToken ct);
	/// <summary> Gets the dates on which no rate exists for the pair (in either direction), e.g. weekends and holidays </summary>
	Task<HashSet<DateOnly>> GetUnavailableDatesAsync(string currency1, string currency2, DateOnly from, DateOnly to, CancellationToken ct);
}