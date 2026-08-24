using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Utilities;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ExchangeRates.Server.Providers;

public sealed class CentralBankProviderFactory {
	private readonly IServiceProvider _services;

	public CentralBankProviderFactory(IServiceProvider services) {
		_services = services;
	}
	public IEnumerable<ICentralBankProvider> GetAll() {
		return _services.GetServices<ICentralBankProvider>();
	}

	public ICentralBankProvider Get(string providerCode)
		=> providerCode.ToUpperInvariant() switch {
			"AMCM" => _services.GetRequiredService<AMCMProvider>(),
			"BAM" => _services.GetRequiredService<BAMProvider>(),
			"BANREP" => _services.GetRequiredService<BANREPProvider>(),
			"BANXICO" => _services.GetRequiredService<BANXICOProvider>(),
			"BBK" => _services.GetRequiredService<BBKProvider>(),
			"BCB" => _services.GetRequiredService<BCBProvider>(),
			"BCBO" => _services.GetRequiredService<BCBOProvider>(),
			"BCC" => _services.GetRequiredService<BCCProvider>(),
			"BCCH" => _services.GetRequiredService<BCCHProvider>(),
			"BCCR" => _services.GetRequiredService<BCCRProvider>(),
			"BCEAO" => _services.GetRequiredService<BCEAOProvider>(),
			"BCN" => _services.GetRequiredService<BCNProvider>(),
			"BCP" => _services.GetRequiredService<BCPProvider>(),
			"BCRA" => _services.GetRequiredService<BCRAProvider>(),
			"BCT" => _services.GetRequiredService<BCTProvider>(),
			"BCU" => _services.GetRequiredService<BCUProvider>(),
			"BDI" => _services.GetRequiredService<BDIProvider>(),
			"BDP" => _services.GetRequiredService<BDPProvider>(),
			"BI" => _services.GetRequiredService<BIProvider>(),
			"BNA" => _services.GetRequiredService<BNAProvider>(),
			"BNM" => _services.GetRequiredService<BNMProvider>(),
			"BNR" => _services.GetRequiredService<BNRProvider>(),
			"BNRRW" => _services.GetRequiredService<BNRRWProvider>(),
			"BOA" => _services.GetRequiredService<BOAProvider>(),
			"BOB" => _services.GetRequiredService<BOBProvider>(),
			"BOC" => _services.GetRequiredService<BOCProvider>(),
			"BOE" => _services.GetRequiredService<BOEProvider>(),
			"BOI" => _services.GetRequiredService<BOIProvider>(),
			"BOJ" => _services.GetRequiredService<BOJProvider>(),
			"BOJA" => _services.GetRequiredService<BOJAProvider>(),
			"BOM" => _services.GetRequiredService<BOMProvider>(),
			"BOT" => _services.GetRequiredService<BOTProvider>(),
			"BOTA" => _services.GetRequiredService<BOTAProvider>(),
			"BRB" => _services.GetRequiredService<BRBProvider>(),
			"BSP" => _services.GetRequiredService<BSPProvider>(),
			"CBA" => _services.GetRequiredService<CBAProvider>(),
			"CBC" => _services.GetRequiredService<CBCProvider>(),
			"CBE" => _services.GetRequiredService<CBEProvider>(),
			"CBG" => _services.GetRequiredService<CBGProvider>(),
			"CBI" => _services.GetRequiredService<CBIProvider>(),
			"CBK" => _services.GetRequiredService<CBKProvider>(),
			"CBLLR" => _services.GetRequiredService<CBLLRProvider>(),
			"CBM" => _services.GetRequiredService<CBMProvider>(),
			"CBN" => _services.GetRequiredService<CBNProvider>(),
			"CBR" => _services.GetRequiredService<CBRProvider>(),
			"CBS" => _services.GetRequiredService<CBSProvider>(),
			"CBSL" => _services.GetRequiredService<CBSLProvider>(),
			"CBU" => _services.GetRequiredService<CBUProvider>(),
			"CNB" => _services.GetRequiredService<CNBProvider>(),
			"DAB" => _services.GetRequiredService<DABProvider>(),
			"DNB" => _services.GetRequiredService<DNBProvider>(),
			"ECB" => _services.GetRequiredService<ECBProvider>(),
			"FBIL" => _services.GetRequiredService<FBILProvider>(),
			"FRED" => _services.GetRequiredService<FREDProvider>(),
			"HKMA" => _services.GetRequiredService<HKMAProvider>(),
			"HNB" => _services.GetRequiredService<HNBProvider>(),
			"IMF" => _services.GetRequiredService<IMFProvider>(),
			"LB" => _services.GetRequiredService<LBProvider>(),
			"MAS" => _services.GetRequiredService<MASProvider>(),
			"MMA" => _services.GetRequiredService<MMAProvider>(),
			"MNB" => _services.GetRequiredService<MNBProvider>(),
			"NB" => _services.GetRequiredService<NBProvider>(),
			"NBC" => _services.GetRequiredService<NBCProvider>(),
			"NBE" => _services.GetRequiredService<NBEProvider>(),
			"NBG" => _services.GetRequiredService<NBGProvider>(),
			"NBK" => _services.GetRequiredService<NBKProvider>(),
			"NBKR" => _services.GetRequiredService<NBKRProvider>(),
			"NBM" => _services.GetRequiredService<NBMProvider>(),
			"NBP" => _services.GetRequiredService<NBPProvider>(),
			"NBRB" => _services.GetRequiredService<NBRBProvider>(),
			"NBRM" => _services.GetRequiredService<NBRMProvider>(),
			"NBT" => _services.GetRequiredService<NBTProvider>(),
			"NBU" => _services.GetRequiredService<NBUProvider>(),
			"NRB" => _services.GetRequiredService<NRBProvider>(),
			"NRBT" => _services.GetRequiredService<NRBTProvider>(),
			"RB" => _services.GetRequiredService<RBProvider>(),
			"RBA" => _services.GetRequiredService<RBAProvider>(),
			"RBF" => _services.GetRequiredService<RBFProvider>(),
			"RBM" => _services.GetRequiredService<RBMProvider>(),
			"RBV" => _services.GetRequiredService<RBVProvider>(),
			"SARB" => _services.GetRequiredService<SARBProvider>(),
			"SBI" => _services.GetRequiredService<SBIProvider>(),
			"SBP" => _services.GetRequiredService<SBPProvider>(),
			"TCMB" => _services.GetRequiredService<TCMBProvider>(),
			_ => throw new ArgumentOutOfRangeException(nameof(providerCode), providerCode, "Unknown central-bank provider.")
		};
}
