using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Mappers;
using ExchangeRates.Server.Models;
using ExchangeRates.Server.Providers;
using ExchangeRates.Server.Utilities;

namespace ExchangeRates.Server.Services;

public sealed class ExchangeRateService : IExchangeRateService {
	#region Class Fields
	private readonly ILogger<ExchangeRateService> _logger;
	private readonly IExchangeRateRepository _repository;
	private readonly CentralBankProviderFactory _providerFactory;
	private readonly string _pivotCurrency;
	#endregion Class Fields
	public ExchangeRateService(ILogger<ExchangeRateService> logger, IExchangeRateRepository repository, CentralBankProviderFactory providerFactory, IConfiguration configuration) {
		_logger = logger;
		_repository = repository;
		_providerFactory = providerFactory;
		_pivotCurrency = configuration["PivotCurrency"] ?? throw new InvalidOperationException("Missing PivotCurrency configuration.");

	}

	/// <summary> Gets the exchange rates for the specified base and quote currencies between the given date range </summary>
	/// <exception cref="ArgumentException"></exception>
	public async Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (fromDate > toDate) {
			throw new ArgumentException("fromDate cannot be greater than toDate.");
		}

		if (baseCurrency == quoteCurrency) {
			return Enumerable.Range(0, toDate.DayNumber - fromDate.DayNumber + 1)
				.Select(i => new ExchangeRateResult(fromDate.AddDays(i), baseCurrency, quoteCurrency, 1m, "IDENTITY"))
				.ToList();
		}

		// Check if the rates are already available in the repository
		var canonicalRates = await _repository.GetRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, ct);

		IEnumerable<ExchangeRateResult> rates = new List<ExchangeRateResult>();

		if (canonicalRates?.Count > 0) {
			rates = canonicalRates.Select(ExchangeRateMapper.ToResult);
		}

		var missingDates = DateRangeHelper.GetMissingDates(fromDate, toDate, rates.Select(x => x.Date));

		List<DateOnly> missingDatesFiltered = new List<DateOnly>();
		if (missingDates.Count > 0) {
			HashSet<DateOnly> unavailableDatesSet = await _repository.GetUnavailableDatesAsync(missingDates.Min(), missingDates.Max(), ct);
			missingDatesFiltered = missingDates.Where(x => !unavailableDatesSet.Contains(x)).ToList();

			_logger.LogInformation("All rates found for {BaseCurrency}/{QuoteCurrency} between {FromDate} and {ToDate}.", baseCurrency, quoteCurrency, fromDate, toDate);
		}

		if (missingDatesFiltered.Count > 0) {
			_logger.LogInformation("Found {Count} missing dates for {BaseCurrency}/{QuoteCurrency} between {FromDate} and {ToDate}.", missingDatesFiltered.Count, baseCurrency, quoteCurrency, fromDate, toDate);

			// If there are missing rates, fetch them from the resolver
			rates = await GetDirectRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, ct);
			string bankProviderCode = rates.FirstOrDefault()?.Provider;
			var rateDates = rates.Select(x => x.Date).ToHashSet();
			if (rates.Any()) {
				_logger.LogInformation("Direct rates found for {BaseCurrency}/{QuoteCurrency} between {FromDate} and {ToDate}.", baseCurrency, quoteCurrency, fromDate, toDate);

				var centralBank = await _repository.GetCentralBankAsync(bankProviderCode, ct);

				//Saved unavailable dates to avoid future unnecessary API calls
				var missingdatesToSave = new List<ExchangeRateUnavailableDateEntity>();
				for (var from = fromDate; from <= toDate; from = from.AddDays(1)) {
					if (!rateDates.Contains(from)) {
						missingdatesToSave.Add(new ExchangeRateUnavailableDateEntity {
							CentralBankId = centralBank.Id,
							BaseCurrency = baseCurrency,
							QuoteCurrency = quoteCurrency,
							UnavailableDate = from,
							Reason = from.DayOfWeek == DayOfWeek.Saturday || from.DayOfWeek == DayOfWeek.Sunday ? "Weekend" : "Possible holiday",
						});
						_logger.LogInformation("Marking {Date} as unavailable for {BaseCurrency}/{QuoteCurrency}.", from, baseCurrency, quoteCurrency);
					}
				}
				await _repository.AddUnavailableDatesAsync(missingdatesToSave, ct);

				var ratesForDbToSave = await OrientToCanonical(rates, ct);
				var currencyEntities = await _repository.GetCurrencyByCodesAsync([baseCurrency, quoteCurrency], ct);

				var baseCurrencyId = currencyEntities.First(x => x.CurrencyCode == baseCurrency).Id;
				var quoteCurrencyId = currencyEntities.First(x => x.CurrencyCode == quoteCurrency).Id;

				var rst = await _repository.AddRangeAsync(ratesForDbToSave.Select(x => ExchangeRateMapper.ToEntity(x, baseCurrencyId, quoteCurrencyId)), ct);

				_logger.LogInformation("Saved {Count} new rates for {BaseCurrency}/{QuoteCurrency} between {FromDate} and {ToDate}.", rst, baseCurrency, quoteCurrency, fromDate, toDate);

				return OrientRates(rates.ToList(), baseCurrency, quoteCurrency);
			}
		}
		else {
			return OrientRates(rates.ToList(), baseCurrency, quoteCurrency);
		}

		_logger.LogInformation("Direct rates not found for {BaseCurrency}/{QuoteCurrency} between {FromDate} and {ToDate}. Attempting triangulation.", baseCurrency, quoteCurrency, fromDate, toDate);
		// If direct rates are not available, fetch triangulated rates
		return await GetTriangulatedRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, ct);
	}


	/// <summary>
	/// Gets the exchange rates from the bank API for the specified base and quote currencies between the given date range. <br />
	/// The direction of the exchange rate is not considered; if the provider's native currency is the quote currency, the inverse of the rate will be returned.<br />
	/// </summary>
	private async Task<IReadOnlyList<ExchangeRateResult>> GetDirectRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		CentralBankEntity bank = await _repository.FindSuitableBankAsync(baseCurrency, quoteCurrency, ct);
		ICentralBankProvider provider = _providerFactory.GetProvider(bank);
		if (provider == null) {
			var message = $"No provider found for {baseCurrency}/{quoteCurrency} - bank: {bank.BankCode}.";
			var ex = new InvalidOperationException(message);
			_logger.LogError(ex, message);
			throw ex;
		}
		var nativeCurrencyCode = bank.NativeCurrency.CurrencyCode;
		var localQuoteCurrency = (provider.NativeCurrencyCode == quoteCurrency) ? baseCurrency : quoteCurrency;

		_logger.LogDebug("Direct provider {BankCode} selected for {BaseCurrency}/{QuoteCurrency}. Native={NativeCurrency}", provider.BankCode, baseCurrency, quoteCurrency, provider.NativeCurrencyCode);

		var rates = await provider.GetRatesAsync(localQuoteCurrency, fromDate, toDate, ct);

		rates = rates.Select(x => new ExchangeRateResult(x.Date, provider.NativeCurrencyCode, localQuoteCurrency, x.Rate, x.Provider))
							.OrderBy(x => x.Date)
							.ToList();

		if (provider.NativeCurrencyCode == quoteCurrency) {
			return OrientRates(rates, baseCurrency, quoteCurrency);
		}

		return rates;
	}

	/// <summary>
	/// Gets the exchange rates for the specified base and quote currencies between the given date range using triangulation through the pivot currency.
	/// </summary>
	private async Task<IReadOnlyList<ExchangeRateResult>> GetTriangulatedRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		_logger.LogInformation("Triangulating rates for {BaseCurrency}/{QuoteCurrency} between {FromDate} and {ToDate} using pivot currency {PivotCurrency}.", baseCurrency, quoteCurrency, fromDate, toDate, _pivotCurrency);
		var baseToPivot = await GetDirectRatesAsync(baseCurrency, _pivotCurrency, fromDate, toDate, ct);
		var pivotToQuote = await GetDirectRatesAsync(_pivotCurrency, quoteCurrency, fromDate, toDate, ct);

		if (baseToPivot.Count == 0 || pivotToQuote.Count == 0) {
			return Array.Empty<ExchangeRateResult>();
		}

		var quoteByDate = pivotToQuote.GroupBy(x => x.Date)
															.ToDictionary(x => x.Key, x => x.First());

		var rates = baseToPivot.GroupBy(x => x.Date)
											.Select(x => x.First())
											.Where(x => quoteByDate.ContainsKey(x.Date))
											.Select(x => {
												var quoteRate = quoteByDate[x.Date];
												return new ExchangeRateResult(x.Date, baseCurrency, quoteCurrency, x.Rate * quoteRate.Rate, $"{x.Provider}+{quoteRate.Provider}");
											})
											.OrderBy(x => x.Date)
											.ToList();
		return rates;
	}


	private IReadOnlyList<ExchangeRateResult> OrientRates(IReadOnlyList<ExchangeRateResult> canonicalRates, string baseCurrency, string quoteCurrency) {
		if (canonicalRates.Count == 0 ||
			(canonicalRates[0].BaseCurrency == baseCurrency && canonicalRates[0].QuoteCurrency == quoteCurrency)) {
			return canonicalRates;
		}

		return canonicalRates.Where(x => x.Rate != 0)
											.Select(x => {
												if (x.BaseCurrency == baseCurrency && x.QuoteCurrency == quoteCurrency) {
													return x;
												}

												if (x.BaseCurrency == quoteCurrency && x.QuoteCurrency == baseCurrency) {
													return new ExchangeRateResult(x.Date, baseCurrency, quoteCurrency, 1m / x.Rate, x.Provider);
												}

												throw new InvalidOperationException($"Rate {x.BaseCurrency}/{x.QuoteCurrency} cannot be oriented as {baseCurrency}/{quoteCurrency}.");
											})
											.ToList();
	}

	/// <summary>
	/// Orients the exchange rates to a canonical form based on the priority of the currencies. <br />
	/// </summary>
	private async Task<List<ExchangeRateResult>> OrientToCanonical(IEnumerable<ExchangeRateResult> rates, CancellationToken ct) {
		var currencyCodes = rates.SelectMany(x => new[] { x.BaseCurrency, x.QuoteCurrency }).Distinct();
		var currencies = await _repository.GetCurrencyByCodesAsync(currencyCodes, ct);

		return rates.Where(x => x.Rate != 0)
			.Select(x => {
				var baseCurrency = currencies.First(c => c.CurrencyCode == x.BaseCurrency);
				var quoteCurrency = currencies.First(c => c.CurrencyCode == x.QuoteCurrency);

				if (baseCurrency.Priority < quoteCurrency.Priority) {
					return x;
				}

				return new ExchangeRateResult(x.Date, x.QuoteCurrency, x.BaseCurrency, 1m / x.Rate, x.Provider);
			})
			.OrderBy(x => x.Date)
			.ToList();
	}

	public async Task<IReadOnlyList<CurrencyModel>> GetCurrenciesAsync(CancellationToken ct) {
		var currencies = await _repository.GetCurrenciesAsync(ct);

		var result = currencies.Select(x => new CurrencyModel(x.CurrencyCode, x.NumericCode, x.Name, x.IsHistoric)).ToList();
		return result;
	}

	/// <summary>
	/// Gets the list of central banks from the repository <br />
	/// </summary>
	public async Task<IReadOnlyList<CentralBankModel>> GetCentralBanksAsync(CancellationToken ct) {
		var centralBanks = await _repository.GetCentralBanksAsync(ct);
		return centralBanks.Select(x => new CentralBankModel(x.BankCode, x.BankName, x.CountryOfOrigin, x.NativeCurrency.CurrencyCode, x.CurrencyId, x.Priority))
										.ToList();
	}

}