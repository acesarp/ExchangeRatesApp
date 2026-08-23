namespace ExchangeRates.Server.Services;

using ExchangeRates.Server;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;

public class ExchangeRateService : IExchangeRateService {
	private readonly IConfiguration _configuration;
	private readonly CentralBankProviderFactory _providerFactory;

	public ExchangeRateService(IConfiguration configuration, CentralBankProviderFactory providerFactory) {
		_configuration = configuration;
		_providerFactory = providerFactory;
	}

	public async Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(string provider, DateOnly? date, CancellationToken ct = default) {
		var section = _configuration.GetSection($"CentralBanks:{provider}");
		if (!section.Exists()) {
			throw new ArgumentException($"Unknown provider: {provider}");
		}

		var bankProvider = _providerFactory.Get(provider);

		return await bankProvider.GetRatesAsync(date, ct);
	}
}