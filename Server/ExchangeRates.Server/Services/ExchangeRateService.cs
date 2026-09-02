namespace ExchangeRates.Server.Services;

using global::ExchangeRates.Domain.Entities;
using global::ExchangeRates.Domain.Enums;
using global::ExchangeRates.Domain.Interfaces;
using global::ExchangeRates.Server.Extensions;
using global::ExchangeRates.Server.Interfaces;
using global::ExchangeRates.Server.Mappers;
using global::ExchangeRates.Server.Providers;
using global::ExchangeRates.Server.Utilities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class ExchangeRateService : IExchangeRateService {
	private readonly IConfiguration _configuration;
	private readonly CentralBankProviderFactory _providerFactory;
	private readonly FixedExchangeRateProvider _fixedExchangeRateProvider;
	private readonly string _pivotCurrency;
	private readonly ILogger<ExchangeRateService> _logger;
	private readonly IExchangeRateRepository _repository;

	public ExchangeRateService(IConfiguration configuration, CentralBankProviderFactory providerFactory, FixedExchangeRateProvider fixedExchangeRateProvider, IExchangeRateRepository repository, ILogger<ExchangeRateService> logger) {
		_configuration = configuration;
		_providerFactory = providerFactory;
		_fixedExchangeRateProvider = fixedExchangeRateProvider;
		_repository = repository;
		_pivotCurrency = configuration["PivotCurrency"] ?? throw new Exception("Invalid PivotCurrency configuration");
		_logger = logger;
	}

	public async Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) {
		_logger.LogInformation("GetRates requested: {From}->{To} baseCurrency {FromDate} quoteCurrency {ToDate}", baseCurrency, quoteCurrency, fromDate, toDate);

		if (baseCurrency == quoteCurrency) {
			_logger.LogInformation("Identity rate path selected for {Currency}", baseCurrency);
			return new List<ExchangeRateResult> { new ExchangeRateResult(fromDate, baseCurrency, quoteCurrency, 1m, "IDENTITY") };
		}

		// 1. Try direct provider
		ICentralBankProvider? bankProvider = FindDirectProvider(baseCurrency, quoteCurrency);

		if (bankProvider != null) {
			_logger.LogInformation("Direct provider selected: {ProviderCode} for {From}->{To}", bankProvider.Code, baseCurrency, quoteCurrency);
			var directRates = await GetProviderRatesAsync(bankProvider, quoteCurrency, fromDate, toDate, ct);
			_logger.LogInformation("Direct provider {ProviderCode} returned {Count} records", bankProvider.Code, directRates.Count);
			return directRates;
		}

		// 2. No direct rate - triangulate
		_logger.LogInformation("No direct provider for {From}->{To}; using triangulation", baseCurrency, quoteCurrency);
		var triangulatedRates = await GetTriangulatedRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, ct);
		_logger.LogInformation("Triangulation returned {Count} records for {From}->{To}", triangulatedRates.Count, baseCurrency, quoteCurrency);
		return triangulatedRates;
	}

	private async Task<IReadOnlyList<ExchangeRateResult>> GetProviderRatesAsync(ICentralBankProvider provider, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		var fetches = await _repository.GetFetchesAsync(provider.Code, provider.NativeCurrency, quoteCurrency, fromDate, toDate, ct);
		var missingRanges = DateRangeHelper.GetMissingRanges(fromDate, toDate, fetches);

		foreach (var range in missingRanges) {
			IReadOnlyList<ExchangeRateResult> rates = await provider.GetRatesAsync(quoteCurrency, range.From, range.To, ct);

			if (rates.Count > 0) {
				await _repository.AddRangeAsync(rates.Select(r => r.ToEntity()), ct);

				await _repository.AddFetchAsync(new ExchangeRateFetch(provider.Code, provider.NativeCurrency, quoteCurrency, fromDate, toDate), ct);
			}
		}

		var result = await _repository.GetAsync(provider.NativeCurrency, quoteCurrency, fromDate, toDate, ct);
		return result.Select(s => s.ToResult()).ToList();
	}

	private bool TryGetDirectRate(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, IReadOnlyList<ExchangeRateResult> rates, out decimal rate) {
		var direct = rates.FirstOrDefault(x => x.BaseCurrency == baseCurrency && x.QuoteCurrency == quoteCurrency);

		if (direct is not null) {
			rate = direct.Rate;
			return true;
		}

		var inverse = rates.FirstOrDefault(x => x.BaseCurrency == quoteCurrency && x.QuoteCurrency == baseCurrency);

		if (inverse is not null && inverse.Rate != 0) {
			rate = 1m / inverse.Rate;
			return true;
		}

		rate = 0;
		return false;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="baseCurrency"></param>
	/// <param name="quoteCurrency"></param>
	/// <returns></returns>
	private ICentralBankProvider? FindDirectProvider(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency) {
		var providers = _providerFactory.GetAllProviders();
		var provider = providers.FirstOrDefault(p => p.NativeCurrency == baseCurrency && p.SupportedCurrencies.Contains(quoteCurrency) ||
																														p.NativeCurrency == quoteCurrency && p.SupportedCurrencies.Contains(baseCurrency));

		if (provider?.NativeCurrency == quoteCurrency) {
			var swap = baseCurrency;
			baseCurrency = quoteCurrency;
			quoteCurrency = swap;
		}

		return provider;
	}

	private ICentralBankProvider? FindPivotProvider(ECurrencyISO currency) {
		var providers = _providerFactory.GetAllProviders().ToList();
		_logger.LogDebug("Evaluating pivot providers for {Currency}. Registered providers: {ProviderCount}", currency, providers.Count);

		var pivotProvider = providers.FirstOrDefault(p => p.Supports(currency)
			&& p.Supports(_pivotCurrency.ToECurrency()));

		if (pivotProvider == null) {
			_logger.LogWarning("No pivot provider found for {Currency} through pivot {PivotCurrency}", currency, _pivotCurrency);
		}
		else {
			_logger.LogDebug("Pivot provider {ProviderCode} selected for {Currency}", pivotProvider.Code, currency);
		}

		return pivotProvider;
	}

	/// <summary>
	/// Attempts to triangulate exchange rates between baseCurrency and quoteCurrency using the pivot currency.
	/// </summary>
	/// <param name="baseCurrency">The base currency.</param>
	/// <param name="quoteCurrency">The quote currency.</param>
	/// <param name="fromDate">The start date for the exchange rate data.</param>
	/// <param name="toDate">The end date for the exchange rate data.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>A list of exchange rate results.</returns>
	/// <exception cref="InvalidOperationException"></exception>
	private async Task<IReadOnlyList<ExchangeRateResult>> GetTriangulatedRatesAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var fromIsFixed = _fixedExchangeRateProvider.TryGetFixedRate(baseCurrency, out var fromFixedRate);
		var toIsFixed = _fixedExchangeRateProvider.TryGetFixedRate(quoteCurrency, out var toFixedRate);

		var fromProvider = fromIsFixed ? null : FindPivotProvider(baseCurrency);
		var toProvider = toIsFixed ? null : FindPivotProvider(quoteCurrency);

		var pivot = _pivotCurrency.ToECurrency();

		// Only throw if both sides exist but providers are missing
		if ((!fromIsFixed && fromProvider is null) && (!toIsFixed && toProvider is null)) {
			_logger.LogError("Triangulation failed: no providers for either side. From={From} To={To} Pivot={Pivot}", baseCurrency, quoteCurrency, _pivotCurrency);
			throw new InvalidOperationException($"Unable to triangulate {baseCurrency}/{quoteCurrency} through {_pivotCurrency}.");
		}

		// If one side has no provider and is not fixed, return empty list (no data available)
		if ((!fromIsFixed && fromProvider is null) || (!toIsFixed && toProvider is null)) {
			_logger.LogWarning("Triangulation incomplete: provider missing on one side. From={From} To={To} Pivot={Pivot}", baseCurrency, quoteCurrency, _pivotCurrency);
			return new List<ExchangeRateResult>();
		}

		_logger.LogInformation("Triangulation providers selected. FromProvider={FromProvider} ToProvider={ToProvider} Pivot={Pivot}", fromProvider?.Code, toProvider?.Code, pivot);

		var fromRates = fromIsFixed
			? BuildFixedRates(baseCurrency, pivot, fromFixedRate, fromDate, toDate)
			: await GetProviderRatesAsync(fromProvider!, baseCurrency, fromDate, toDate, ct);

		var toRates = toIsFixed
			? BuildFixedRates(pivot, quoteCurrency, 1m / toFixedRate, fromDate, toDate)
			: await GetProviderRatesAsync(toProvider!, quoteCurrency, fromDate, toDate, ct);

		_logger.LogDebug("Triangulation source rates: fromRates={FromCount}, toRates={ToCount}", fromRates.Count, toRates.Count);

		var rates = new List<ExchangeRateResult>();

		if (toRates != null && fromRates != null) {
			foreach (var date in fromRates.Select(x => x.Date).Intersect(toRates.Select(x => x.Date)).Order()) {
				var fromRatesForDate = fromRates.Where(x => x.Date == date).ToList();
				var toRatesForDate = toRates.Where(x => x.Date == date).ToList();

				if (!TryGetDirectRate(baseCurrency, pivot, fromRatesForDate, out var fromRate) ||
					!TryGetDirectRate(pivot, quoteCurrency, toRatesForDate, out var toRate)) {
					continue;
				}

				rates.Add(new ExchangeRateResult(date, baseCurrency, quoteCurrency, fromRate * toRate, $"{fromProvider?.Code}+{toProvider?.Code}"));
			}
		}

		_logger.LogInformation("Triangulation completed with {Count} records for {From}->{To}", rates.Count, baseCurrency, quoteCurrency);
		return rates;
	}

	private static IReadOnlyList<ExchangeRateResult> BuildFixedRates(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, decimal rate, DateOnly fromDate, DateOnly toDate) {
		var rates = new List<ExchangeRateResult>();
		for (var d = fromDate; d <= toDate; d = d.AddDays(1)) {
			rates.Add(new ExchangeRateResult(d, baseCurrency, quoteCurrency, rate, "FIXED"));
		}
		return rates;
	}
}