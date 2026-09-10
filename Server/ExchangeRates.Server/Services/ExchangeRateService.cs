using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Server.Extensions;
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
	private IReadOnlyList<CurrencyEntity>? _currencies;
	#endregion Class Fields
	public ExchangeRateService(ILogger<ExchangeRateService> logger, IExchangeRateRepository repository, CentralBankProviderFactory providerFactory, IConfiguration configuration) {
		_logger = logger;
		_repository = repository;
		_providerFactory = providerFactory;
		_pivotCurrency = configuration["Priority"]?.ToECurrency() ?? throw new InvalidOperationException("Missing Priority configuration.");
	}


	/// <summary>
	/// Gets the exchange rates for the specified base and quote currencies between the given date range. <br />
	/// </summary>
	/// <param name="baseCurrency">The base currency.</param>
	/// <param name="quoteCurrency">The quote currency.</param>
	/// <param name="fromDate">The start date of the range.</param>
	/// <param name="toDate">The end date of the range.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>A list of exchange rate results.</returns>
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
			rates = canonicalRates.Select(ExchangeRateMapper.ToResult)
												.ToList();
		}

		var missingRanges = DateRangeHelper.GetMissingRanges(fromDate, toDate, rates.Select(x => x.Date).ToList());

		if (missingRanges.Count > 0) {
			_logger.LogInformation("Found {Count} missing ranges for {BaseCurrency}/{QuoteCurrency} between {FromDate} and {ToDate}.", missingRanges.Count, baseCurrency, quoteCurrency, fromDate, toDate);

			// If there are missing rates, fetch them from the resolver
			rates = await GetDirectRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, ct);

			if (rates.Any()) {
				_logger.LogInformation("Direct rates found for {BaseCurrency}/{QuoteCurrency} between {FromDate} and {ToDate}.", baseCurrency, quoteCurrency, fromDate, toDate);
				var ratesForDbToSave = OrientToCanonical(rates);
				await _repository.AddRangeAsync(ratesForDbToSave.Select(ExchangeRateMapper.ToEntity), ct);

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
	/// Gets the exchange rates for the specified base and quote currencies between the given date range. <br />
	/// The direction of the exchange rate is not considered; if the provider's native currency is the quote currency, the inverse of the rate will be returned.<br />
	/// </summary>
	private async Task<IReadOnlyList<ExchangeRateResult>> GetDirectRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var provider = FindProvider(baseCurrency, quoteCurrency);

		if (provider == null) {
			return Array.Empty<ExchangeRateResult>();
		}

		var localQuoteCurrency = (provider.NativeCurrencyCode == quoteCurrency) ? baseCurrency : quoteCurrency;

		_logger.LogDebug("Direct provider {BankCode} selected for {baseCurrency}/{quoteCurrency}. Native={NativeCurrency}", provider.BankCode, baseCurrency, quoteCurrency, provider.NativeCurrencyCode);

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

	/// <summary>
	/// - Finds a provider that supports the given currencies; <br />
	/// - Exchange rates are considered bidirectional;  <br />
	/// - Exchange rate direction is not guaranteed; the provider may support either currency as its native currency. <br />
	/// </summary>
	private ICentralBankProvider? FindProvider(string currency1, string currency2) {

		var preferredProvider = _providerFactory.GetPreferredProviders()
																			.FirstOrDefault(p => (p.NativeCurrencyCode == currency1 && p.SupportedCurrencies.Contains(currency2)) ||
																									(p.NativeCurrencyCode == currency2 && p.SupportedCurrencies.Contains(currency1)));

		return preferredProvider ?? _providerFactory.GetAllProviders()
																				.FirstOrDefault(p => (p.NativeCurrencyCode == currency1 && p.SupportedCurrencies.Contains(currency2)) ||
																											(p.NativeCurrencyCode == currency2 && p.SupportedCurrencies.Contains(currency1)));
	}


	private static IReadOnlyList<ExchangeRateResult> OrientRates(IReadOnlyList<ExchangeRateResult> canonicalRates, string baseCurrency, string quoteCurrency) {
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


	private static IReadOnlyList<ExchangeRateResult> OrientToCanonical(
		IEnumerable<ExchangeRateResult> rates,
		IReadOnlyList<CurrencyEntity> currencies) {

		return rates
			.Where(x => x.Rate != 0)
			.Select(x => {
				var baseCurrency = currencies.First(c => c.CurrencyCode == x.BaseCurrency);
				var quoteCurrency = currencies.First(c => c.CurrencyCode == x.QuoteCurrency);

				if (baseCurrency.Priority < quoteCurrency.Priority) {
					return x;
				}

				return new ExchangeRateResult(
					x.Date,
					x.QuoteCurrency,
					x.BaseCurrency,
					1m / x.Rate,
					x.Provider);
			})
			.OrderBy(x => x.Date)
			.ToList();
	}

	private async Task<IReadOnlyList<CurrencyEntity>> GetCurrenciesCachedAsync(CancellationToken ct) {
		if (_currencies != null) {
			return _currencies;
		}

		_currencies = await _repository.GetCurrenciesAsync(ct);
		return _currencies;
	}

	public async Task<IReadOnlyList<CurrencyModel>> GetCurrenciesAsync(CancellationToken ct) {
		var currencies = await GetCurrenciesCachedAsync(ct);
		return currencies.Select(x => new CurrencyModel(x.CurrencyCode, x.NumericCode, x.Name, x.IsHistoric, x.Priority)).ToList();
	}

	public async Task<IReadOnlyList<CentralBankModel>> GetCentralBanksAsync(CancellationToken ct) {
		var centralBanks = await _repository.GetCentralBanksAsync(ct);
		return centralBanks.Select(x => new CentralBankModel(x.BankCode, x.BankName, x.CountryOfOrigin, x.Currency.CurrencyCode, x.CurrencyId, x.Priority))
										.ToList();
	}
}