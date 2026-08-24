namespace ExchangeRates.Server.Services;

using ExchangeRates.Server;
using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;

public class ExchangeRateService : IExchangeRateService {
	private readonly IConfiguration _configuration;
	private readonly CentralBankProviderFactory _providerFactory;

	public ExchangeRateService(IConfiguration configuration, CentralBankProviderFactory providerFactory) {
		_configuration = configuration;
		_providerFactory = providerFactory;
	}

	public async Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(ECurrency fromCurrency, ECurrency toCurrency, DateOnly? date, CancellationToken ct = default) {

		var section = _configuration.GetSection($"CentralBanks:{provider}");
		if (!section.Exists()) {
			throw new ArgumentException($"Unknown provider: {provider}");
		}

		var bankProvider = _providerFactory.Get(provider);

		return await bankProvider.GetRatesAsync(date, ct);
	}

	private static readonly string[] PivotCurrencies = { "USD", "EUR", "GBP" };

	private decimal GetRate(string fromCurrency, string toCurrency, IReadOnlyList<ExchangeRate> rates) {
		if (fromCurrency == toCurrency) {
			return 1m;
		}

		if (TryGetDirectRate(fromCurrency, toCurrency, rates, out var directRate)) {
			return directRate;
		}

		foreach (var pivot in PivotCurrencies) {
			if (TryGetDirectRate(fromCurrency, pivot, rates, out var fromRate) && TryGetDirectRate(pivot, toCurrency, rates, out var toRate)) {
				return fromRate * toRate;
			}
		}

		throw new InvalidOperationException($"Unable to calculate {fromCurrency}/{toCurrency}.");
	}

	private bool TryGetDirectRate(string fromCurrency, string toCurrency, IReadOnlyList<ExchangeRate> rates, out decimal rate) {
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

	private ICentralBankProvider? FindProvider(ECurrency from, ECurrency to) {
		return _providerFactory.GetAll()
			.FirstOrDefault(p => p.SupportedCurrencies.Contains(from) && p.SupportedCurrencies.Contains(to));
	}
}