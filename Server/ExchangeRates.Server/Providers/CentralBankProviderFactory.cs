using ExchangeRates.Server.Interfaces;

namespace ExchangeRates.Server.Providers;

public sealed class CentralBankProviderFactory {
	private readonly IEnumerable<ICentralBankProvider> _providers;
	private readonly ILogger<CentralBankProviderFactory> _logger;
	private readonly IConfiguration _configuration;

	public CentralBankProviderFactory(IConfiguration configuration, IEnumerable<ICentralBankProvider> providers, ILogger<CentralBankProviderFactory> logger) {
		_configuration = configuration;
		_providers = providers;
		_logger = logger;
	}

	public IEnumerable<ICentralBankProvider> GetAllProviders() {
		var providers = _providers.ToList();
		_logger.LogDebug("Resolved {Count} central bank providers", providers.Count);
		return providers;
	}

	/// <summary>
	/// Preferred-provider tier bank list<br />
	/// Returns the preferred central bank providers.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<ICentralBankProvider> GetPreferredProviders() {

		var providers = _providers.Where(provider => provider.Priority.HasValue)
													.OrderBy(provider => provider.Priority!.Value)
													.ToList();

		_logger.LogDebug("Resolved {Count} central bank providers", providers.Count);

		return providers;

	}

	public ICentralBankProvider GetProvider(string providerBankCode) {
		return GetAllProviders()
					.FirstOrDefault(provider => string.Equals(provider.BankCode, providerBankCode, StringComparison.OrdinalIgnoreCase)) ??
																		throw new ArgumentOutOfRangeException(nameof(providerBankCode), providerBankCode, "Unknown central-bank provider.");
	}
}
