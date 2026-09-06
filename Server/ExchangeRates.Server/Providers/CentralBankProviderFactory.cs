using ExchangeRates.Server.Interfaces;

namespace ExchangeRates.Server.Providers;

public sealed class CentralBankProviderFactory {
	private readonly IEnumerable<ICentralBankProvider> _providers;
	private readonly ILogger<CentralBankProviderFactory> _logger;
	private readonly IConfiguration _configuration;
	private readonly IEnumerable<string> preferredProviders;


	public CentralBankProviderFactory(IConfiguration configuration, IEnumerable<ICentralBankProvider> providers, ILogger<CentralBankProviderFactory> logger) {
		_configuration = configuration;
		_providers = providers;
		_logger = logger;
		preferredProviders = _configuration.GetSection("PreferredProviders").Get<IEnumerable<string>>() ?? Enumerable.Empty<string>();
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

		var providers = preferredProviders.Select(providerCode => _providers.FirstOrDefault(provider => string.Equals(provider.Code, providerCode, StringComparison.OrdinalIgnoreCase)))
																.Where(provider => provider != null)
																.ToList();

		_logger.LogDebug("Resolved {Count} central bank providers", providers.Count);
		return providers!;
	}

	public ICentralBankProvider GetProvider(string providerCode) {
		return GetAllProviders().FirstOrDefault(provider => string.Equals(provider.Code, providerCode, StringComparison.OrdinalIgnoreCase)) ??
																		throw new ArgumentOutOfRangeException(nameof(providerCode), providerCode, "Unknown central-bank provider.");
	}
}
