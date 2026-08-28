using ExchangeRates.Server.Interfaces;

namespace ExchangeRates.Server.Providers;

public sealed class CentralBankProviderFactory {
	private readonly IEnumerable<ICentralBankProvider> _providers;
	private readonly ILogger<CentralBankProviderFactory> _logger;

	public CentralBankProviderFactory(IEnumerable<ICentralBankProvider> providers, ILogger<CentralBankProviderFactory> logger) {
		_providers = providers;
		_logger = logger;
	}

	public IEnumerable<ICentralBankProvider> GetAllProviders() {
		var providers = _providers.ToList();
		_logger.LogDebug("Resolved {Count} central bank providers", providers.Count);
		return providers;
	}

	public ICentralBankProvider GetProvider(string providerCode)
		=> GetAllProviders().FirstOrDefault(provider =>
				string.Equals(provider.Code, providerCode, StringComparison.OrdinalIgnoreCase))
			?? throw new ArgumentOutOfRangeException(nameof(providerCode), providerCode, "Unknown central-bank provider.");
}
