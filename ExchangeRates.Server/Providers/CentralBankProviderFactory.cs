using ExchangeRates.Server.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExchangeRates.Server.Providers;

public sealed class CentralBankProviderFactory {
	private readonly IEnumerable<ICentralBankProvider> _providers;
	private readonly ILogger<CentralBankProviderFactory> _logger;

	public CentralBankProviderFactory(IEnumerable<ICentralBankProvider> providers, ILogger<CentralBankProviderFactory> logger) {
		_providers = providers;
		_logger = logger;
	}

	public IEnumerable<ICentralBankProvider> GetAll() {
		var providers = _providers.ToList();
		_logger.LogDebug("Resolved {Count} central bank providers", providers.Count);
		return providers;
	}

	public ICentralBankProvider Get(string providerCode)
		=> GetAll().FirstOrDefault(provider =>
				string.Equals(provider.Code, providerCode, StringComparison.OrdinalIgnoreCase))
			?? throw new ArgumentOutOfRangeException(nameof(providerCode), providerCode, "Unknown central-bank provider.");
}
