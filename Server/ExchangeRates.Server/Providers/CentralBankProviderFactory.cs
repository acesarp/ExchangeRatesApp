using ExchangeRates.Domain.Entities;
using ExchangeRates.Server.Interfaces;

namespace ExchangeRates.Server.Providers;

public sealed class CentralBankProviderFactory {
	private readonly IEnumerable<ICentralBankProvider> _providers;
	private readonly ILogger<CentralBankProviderFactory> _logger;
	private readonly IConfiguration _configuration;
	private readonly IServiceProvider _services;
	private readonly Dictionary<string, Func<CentralBankEntity, ICentralBankProvider>> _providerFactories;

	public CentralBankProviderFactory(IConfiguration configuration, IEnumerable<ICentralBankProvider> providers, IServiceProvider services, ILogger<CentralBankProviderFactory> logger) {
		_configuration = configuration;
		_providers = providers;
		_logger = logger;
		_services = services;

		_providerFactories = new Dictionary<string, Func<CentralBankEntity, ICentralBankProvider>>(StringComparer.OrdinalIgnoreCase) {
			["AFA"] = Create<AFAProvider>(),
			["AMCM"] = Create<AMCMProvider>(),
			["BAM"] = Create<BAMProvider>(),
			["BANREP"] = Create<BANREPProvider>(),
			["BANXICO"] = Create<BANXICOProvider>(),
			["BBK"] = Create<BBKProvider>(),
			["BCB"] = Create<BCBProvider>(),
			["BCBO"] = Create<BCBOProvider>(),
			["BCC"] = Create<BCCProvider>(),
			["BOK"] = Create<BOKProvider>(),
			["SNB"] = Create<SNBProvider>(),
			["BCCR"] = Create<BCCRProvider>(),
			["BCEAO"] = Create<BCEAOProvider>(),
			["BCN"] = Create<BCNProvider>(),
			["BCP"] = Create<BCPProvider>(),
			["BCRA"] = Create<BCRAProvider>(),
			["BCT"] = Create<BCTProvider>(),
			["BCU"] = Create<BCUProvider>(),
			["BDI"] = Create<BDIProvider>(),
			["BDP"] = Create<BDPProvider>(),
			["BI"] = Create<BIProvider>(),
			["BNA"] = Create<BNAProvider>(),
			["BNM"] = Create<BNMProvider>(),
			["BNR"] = Create<BNRProvider>(),
			["BNRRW"] = Create<BNRRWProvider>(),
			["BOA"] = Create<BOAProvider>(),
			["BOB"] = Create<BOBProvider>(),
			["BOC"] = Create<BOCProvider>(),
			["BOE"] = Create<BOEProvider>(),
			["BOI"] = Create<BOIProvider>(),
			["BOJ"] = Create<BOJProvider>(),
			["BOJA"] = Create<BOJAProvider>(),
			["BOM"] = Create<BOMProvider>(),
			["BOT"] = Create<BOTProvider>(),
			["BOTA"] = Create<BOTAProvider>(),
			["BRB"] = Create<BRBProvider>(),
			["BSP"] = Create<BSPProvider>(),
			["CBA"] = Create<CBAProvider>(),
			["CBC"] = Create<CBCProvider>(),
			["CBE"] = Create<CBEProvider>(),
			["CBG"] = Create<CBGProvider>(),
			["CBI"] = Create<CBIProvider>(),
			["CBK"] = Create<CBKProvider>(),
			["CBLLR"] = Create<CBLLRProvider>(),
			["CBM"] = Create<CBMProvider>(),
			["CBN"] = Create<CBNProvider>(),
			["CBR"] = Create<CBRProvider>(),
			["CBS"] = Create<CBSProvider>(),
			["CBSL"] = Create<CBSLProvider>(),
			["CBU"] = Create<CBUProvider>(),
			["CNB"] = Create<CNBProvider>(),
			["DAB"] = Create<DABProvider>(),
			["DNB"] = Create<DNBProvider>(),
			["ECB"] = Create<ECBProvider>(),
			["FBIL"] = Create<FBILProvider>(),
			["FRED"] = Create<FREDProvider>(),
			["HKMA"] = Create<HKMAProvider>(),
			["HNB"] = Create<HNBProvider>(),
			["IMF"] = Create<IMFProvider>(),
			["LB"] = Create<LBProvider>(),
			["MAS"] = Create<MASProvider>(),
			["MMA"] = Create<MMAProvider>(),
			["MNB"] = Create<MNBProvider>(),
			["NB"] = Create<NBProvider>(),
			["NBC"] = Create<NBCProvider>(),
			["NBE"] = Create<NBEProvider>(),
			["NBG"] = Create<NBGProvider>(),
			["NBK"] = Create<NBKProvider>(),
			["NBKR"] = Create<NBKRProvider>(),
			["NBM"] = Create<NBMProvider>(),
			["NBP"] = Create<NBPProvider>(),
			["NBRB"] = Create<NBRBProvider>(),
			["NBRM"] = Create<NBRMProvider>(),
			["NBT"] = Create<NBTProvider>(),
			["NBU"] = Create<NBUProvider>(),
			["NRB"] = Create<NRBProvider>(),
			["NRBT"] = Create<NRBTProvider>(),
			["RB"] = Create<RBProvider>(),
			["RBA"] = Create<RBAProvider>(),
			["RBF"] = Create<RBFProvider>(),
			["RBM"] = Create<RBMProvider>(),
			["RBV"] = Create<RBVProvider>(),
			["SARB"] = Create<SARBProvider>(),
			["SBI"] = Create<SBIProvider>(),
			["SBP"] = Create<SBPProvider>(),
			["TCMB"] = Create<TCMBProvider>(),
			["CBUAE"] = Create<CBUAEProvider>()
		};
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



	private Func<CentralBankEntity, ICentralBankProvider> Create<TProvider>() where TProvider : class, ICentralBankProvider {

		return bank => ActivatorUtilities.CreateInstance<TProvider>(_services, bank);
	}

}
