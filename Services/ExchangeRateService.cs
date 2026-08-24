namespace ExchangeRates.Server.Services;

using global::ExchangeRates.Server.Enums;
using global::ExchangeRates.Server.Extensions;
using global::ExchangeRates.Server.Interfaces;
using global::ExchangeRates.Server.Providers;

public class ExchangeRateService : IExchangeRateService {
	private readonly IConfiguration _configuration;
	private readonly CentralBankProviderFactory _providerFactory;
	private readonly FixedExchangeRateProvider _fixedExchangeRateProvider;
	private readonly string _pivotCurrency;

	public ExchangeRateService(IConfiguration configuration, CentralBankProviderFactory providerFactory, FixedExchangeRateProvider fixedExchangeRateProvider) {
		_configuration = configuration;
		_providerFactory = providerFactory;
		_fixedExchangeRateProvider = fixedExchangeRateProvider;
		_pivotCurrency = configuration["PivotCurrency"] ?? throw new Exception("Invalid PivotCurrency configuration");
	}

	public async Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(ECurrencyISO from, ECurrencyISO to, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default) {
		var _fromDate = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
		var _toDate = toDate ?? _fromDate;

		if (from == to) {
			return new List<ExchangeRate> { new ExchangeRate(_fromDate, from, to, 1m, "IDENTITY") };
		}
		var result = new List<ExchangeRate>();
		// 1. Try direct provider
		ICentralBankProvider? bankProvider = FindDirectProvider(from, to);

		if (bankProvider != null) {
			return await bankProvider.GetRatesAsync(to, _fromDate, _toDate, ct);
		}

		// 2. No direct rate - triangulate
		return await GetTriangulatedRatesAsync(from, to, _fromDate, _toDate, ct);
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
		var rst = _providerFactory.GetAll();
		var rst1 = rst.FirstOrDefault(p => p.Supports(currency)
																																	&& p.Supports(Enum.Parse<ECurrencyISO>(_pivotCurrency)));
		return rst1;
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
		var fromIsFixed = _fixedExchangeRateProvider.TryGetRate(from, out var fromFixedRate);
		var toIsFixed = _fixedExchangeRateProvider.TryGetRate(to, out var toFixedRate);

		var fromProvider = fromIsFixed ? null : FindPivotProvider(from);
		var toProvider = toIsFixed ? null : FindPivotProvider(to);

		var pivot = _pivotCurrency.ToECurrency();

		if (fromProvider is null || toProvider is null) {
			throw new InvalidOperationException($"Unable to triangulate {from}/{to} through {_pivotCurrency}.");
		}

		var fromRates = await fromProvider.GetRatesAsync(from, fromDate, toDate, ct);
		var toRates = fromProvider == toProvider ? fromRates : await toProvider.GetRatesAsync(to, fromDate, toDate, ct);
		var rates = new List<ExchangeRate>();

		foreach (var date in fromRates.Select(x => x.date).Intersect(toRates.Select(x => x.date)).Order()) {
			var fromRatesForDate = fromRates.Where(x => x.date == date).ToList();
			var toRatesForDate = toRates.Where(x => x.date == date).ToList();

			if (!TryGetDirectRate(from, pivot, fromRatesForDate, out var fromRate) ||
				!TryGetDirectRate(pivot, to, toRatesForDate, out var toRate)) {
				continue;
			}

			rates.Add(new ExchangeRate(date, from, to, fromRate * toRate, $"{fromProvider.Code}+{toProvider.Code}"));
		}

		return rates;
	}
}
