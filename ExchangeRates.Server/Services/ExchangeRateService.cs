namespace ExchangeRates.Server.Services;

using global::ExchangeRates.Server.Enums;
using global::ExchangeRates.Server.Extensions;
using global::ExchangeRates.Server.Interfaces;
using global::ExchangeRates.Server.Providers;

using Microsoft.Extensions.Logging;

public class ExchangeRateService : IExchangeRateService {
	private readonly IConfiguration _configuration;
	private readonly CentralBankProviderFactory _providerFactory;
	private readonly FixedExchangeRateProvider _fixedExchangeRateProvider;
	private readonly string _pivotCurrency;
	private readonly ILogger<ExchangeRateService> _logger;

	public ExchangeRateService(IConfiguration configuration, CentralBankProviderFactory providerFactory, FixedExchangeRateProvider fixedExchangeRateProvider, ILogger<ExchangeRateService> logger) {
		_configuration = configuration;
		_providerFactory = providerFactory;
		_fixedExchangeRateProvider = fixedExchangeRateProvider;
		_pivotCurrency = configuration["PivotCurrency"] ?? throw new Exception("Invalid PivotCurrency configuration");
		_logger = logger;
	}

	public async Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(ECurrencyISO from, ECurrencyISO to, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default) {
		var _fromDate = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
		var _toDate = toDate ?? _fromDate;

		_logger.LogInformation("GetRates requested: {From}->{To} from {FromDate} to {ToDate}", from, to, _fromDate, _toDate);

		if (from == to) {
			_logger.LogInformation("Identity rate path selected for {Currency}", from);
			return new List<ExchangeRate> { new ExchangeRate(_fromDate, from, to, 1m, "IDENTITY") };
		}

		// 1. Try direct provider
		ICentralBankProvider? bankProvider = FindDirectProvider(from, to);

		if (bankProvider != null) {
			_logger.LogInformation("Direct provider selected: {ProviderCode} for {From}->{To}", bankProvider.Code, from, to);
			var directRates = await bankProvider.GetRatesAsync(to, _fromDate, _toDate, ct);
			_logger.LogInformation("Direct provider {ProviderCode} returned {Count} records", bankProvider.Code, directRates.Count);
			return directRates;
		}

		// 2. No direct rate - triangulate
		_logger.LogInformation("No direct provider for {From}->{To}; using triangulation", from, to);
		var triangulatedRates = await GetTriangulatedRatesAsync(from, to, _fromDate, _toDate, ct);
		_logger.LogInformation("Triangulation returned {Count} records for {From}->{To}", triangulatedRates.Count, from, to);
		return triangulatedRates;
	}

	private bool TryGetDirectRate(ECurrencyISO fromCurrency, ECurrencyISO toCurrency, IReadOnlyList<ExchangeRate> rates, out decimal rate) {
		var direct = rates.FirstOrDefault(x => x.BaseCurrency == fromCurrency && x.QuoteCurrency == toCurrency);

		if (direct is not null) {
			rate = direct.Rate;
			return true;
		}

		var inverse = rates.FirstOrDefault(x => x.BaseCurrency == toCurrency && x.QuoteCurrency == fromCurrency);

		if (inverse is not null && inverse.Rate != 0) {
			rate = 1m / inverse.Rate;
			return true;
		}

		rate = 0;
		return false;
	}

	private ICentralBankProvider? FindDirectProvider(ECurrencyISO from, ECurrencyISO to) {
		return _providerFactory.GetAll().FirstOrDefault(p => p.Supports(from) && p.Supports(to) &&
																											(p.NativeCurrency == from || p.NativeCurrency == to));
	}

	private ICentralBankProvider? FindPivotProvider(ECurrencyISO currency) {
		var providers = _providerFactory.GetAll().ToList();
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
	/// 
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="fromDate"></param>
	/// <param name="toDate"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	private async Task<IReadOnlyList<ExchangeRate>> GetTriangulatedRatesAsync(ECurrencyISO from, ECurrencyISO to, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var fromIsFixed = _fixedExchangeRateProvider.TryGetFixedRate(from, out var fromFixedRate);
		var toIsFixed = _fixedExchangeRateProvider.TryGetFixedRate(to, out var toFixedRate);

		var fromProvider = fromIsFixed ? null : FindPivotProvider(from);
		var toProvider = toIsFixed ? null : FindPivotProvider(to);

		var pivot = _pivotCurrency.ToECurrency();

		// Only throw if both sides exist but providers are missing
		if ((!fromIsFixed && fromProvider is null) && (!toIsFixed && toProvider is null)) {
			_logger.LogError("Triangulation failed: no providers for either side. From={From} To={To} Pivot={Pivot}", from, to, _pivotCurrency);
			throw new InvalidOperationException($"Unable to triangulate {from}/{to} through {_pivotCurrency}.");
		}

		// If one side has no provider and is not fixed, return empty list (no data available)
		if ((!fromIsFixed && fromProvider is null) || (!toIsFixed && toProvider is null)) {
			_logger.LogWarning("Triangulation incomplete: provider missing on one side. From={From} To={To} Pivot={Pivot}", from, to, _pivotCurrency);
			return new List<ExchangeRate>();
		}

		_logger.LogInformation("Triangulation providers selected. FromProvider={FromProvider} ToProvider={ToProvider} Pivot={Pivot}", fromProvider?.Code, toProvider?.Code, pivot);

		var fromRates = fromIsFixed
			? BuildFixedRates(from, pivot, fromFixedRate, fromDate, toDate)
			: await fromProvider!.GetRatesAsync(from, fromDate, toDate, ct);

		var toRates = toIsFixed
			? BuildFixedRates(pivot, to, 1m / toFixedRate, fromDate, toDate)
			: await toProvider!.GetRatesAsync(to, fromDate, toDate, ct);

		_logger.LogDebug("Triangulation source rates: fromRates={FromCount}, toRates={ToCount}", fromRates.Count, toRates.Count);

		var rates = new List<ExchangeRate>();

		if (toRates != null && fromRates != null) {
			foreach (var date in fromRates.Select(x => x.date).Intersect(toRates.Select(x => x.date)).Order()) {
				var fromRatesForDate = fromRates.Where(x => x.date == date).ToList();
				var toRatesForDate = toRates.Where(x => x.date == date).ToList();

				if (!TryGetDirectRate(from, pivot, fromRatesForDate, out var fromRate) ||
					!TryGetDirectRate(pivot, to, toRatesForDate, out var toRate)) {
					continue;
				}

				rates.Add(new ExchangeRate(date, from, to, fromRate * toRate, $"{fromProvider?.Code}+{toProvider?.Code}"));
			}
		}

		_logger.LogInformation("Triangulation completed with {Count} records for {From}->{To}", rates.Count, from, to);
		return rates;
	}

	private static IReadOnlyList<ExchangeRate> BuildFixedRates(ECurrencyISO from, ECurrencyISO to, decimal rate, DateOnly fromDate, DateOnly toDate) {
		var rates = new List<ExchangeRate>();
		for (var d = fromDate; d <= toDate; d = d.AddDays(1)) {
			rates.Add(new ExchangeRate(d, from, to, rate, "FIXED"));
		}
		return rates;
	}
}