using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Extensions;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;

namespace ExchangeRates.Server.Services;

public sealed class ExchangeRateResolver {
	private readonly CentralBankProviderFactory _providerFactory;
	private readonly ECurrencyISO _pivotCurrency;
	private readonly ILogger<ExchangeRateResolver> _logger;

	public ExchangeRateResolver(IConfiguration configuration, CentralBankProviderFactory providerFactory, ILogger<ExchangeRateResolver> logger) {
		_providerFactory = providerFactory;
		_logger = logger;
		_pivotCurrency = configuration["PivotCurrency"]?.ToECurrency() ?? throw new InvalidOperationException("Missing PivotCurrency configuration.");
	}

	public async Task<IReadOnlyList<ExchangeRateResult>> GetDirectRatesAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var provider = FindDirectProvider(baseCurrency, quoteCurrency);

		if (provider == null) {
			return Array.Empty<ExchangeRateResult>();
		}

		var inverse = provider.NativeCurrency == quoteCurrency;
		var providerQuoteCurrency = inverse ? baseCurrency : quoteCurrency;

		_logger.LogDebug("Direct provider {Code} selected for {baseCurrency}/{quoteCurrency}. Native={NativeCurrency}, Inverse={inverse}", provider.Code, baseCurrency, quoteCurrency, provider.NativeCurrency, inverse);

		var rates = await provider.GetRatesAsync(providerQuoteCurrency, fromDate, toDate, ct);

		if (!inverse) {
			return rates.Select(x => new ExchangeRateResult(x.Date, baseCurrency, quoteCurrency, x.Rate, x.Provider))
								.OrderBy(x => x.Date)
								.ToList();
		}

		return rates.Where(x => x.Rate != 0)
							.Select(x => new ExchangeRateResult(x.Date, baseCurrency, quoteCurrency, 1m / x.Rate, x.Provider))
							.OrderBy(x => x.Date)
							.ToList();
	}

	public async Task<IReadOnlyList<ExchangeRateResult>> GetTriangulatedRatesAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var baseToPivot = await GetLegRatesAsync(baseCurrency, _pivotCurrency, fromDate, toDate, ct);
		var pivotToQuote = await GetLegRatesAsync(_pivotCurrency, quoteCurrency, fromDate, toDate, ct);

		if (baseToPivot.Count == 0 || pivotToQuote.Count == 0) {
			return Array.Empty<ExchangeRateResult>();
		}

		var quoteByDate = pivotToQuote.GroupBy(x => x.Date)
															.ToDictionary(x => x.Key, x => x.First());

		return baseToPivot.GroupBy(x => x.Date)
										.Select(x => x.First())
										.Where(x => quoteByDate.ContainsKey(x.Date))
										.Select(x => {
											var quoteRate = quoteByDate[x.Date];
											return new ExchangeRateResult(x.Date, baseCurrency, quoteCurrency, x.Rate * quoteRate.Rate, $"{x.Provider}+{quoteRate.Provider}");
										})
										.OrderBy(x => x.Date)
										.ToList();
	}

	private async Task<IReadOnlyList<ExchangeRateResult>> GetLegRatesAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (baseCurrency == quoteCurrency) {
			return Enumerable.Range(0, toDate.DayNumber - fromDate.DayNumber + 1)
											.Select(i => new ExchangeRateResult(fromDate.AddDays(i), baseCurrency, quoteCurrency, 1m, "IDENTITY"))
											.ToList();
		}

		return await GetDirectRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, ct);
	}

	private ICentralBankProvider? FindDirectProvider(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency) {

		var preferredProvider = _providerFactory.GetPreferredProviders()
																			.FirstOrDefault(p => (p.NativeCurrency == baseCurrency && p.SupportedCurrencies.Contains(quoteCurrency)) ||
																									(p.NativeCurrency == quoteCurrency && p.SupportedCurrencies.Contains(baseCurrency)));

		return preferredProvider ?? _providerFactory.GetAllProviders()
																				.FirstOrDefault(p => (p.NativeCurrency == baseCurrency && p.SupportedCurrencies.Contains(quoteCurrency)) ||
																											(p.NativeCurrency == quoteCurrency && p.SupportedCurrencies.Contains(baseCurrency)));
	}
}