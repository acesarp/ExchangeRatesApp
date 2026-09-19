using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Infrastructure;
using ExchangeRates.Infrastructure.Repositories;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace ExchangeRates.Server.Tests.Integration;

[Collection("CentralBanksDataApiTests")]
[Trait("Category", "Integration")]
[CollectionDefinition("CentralBanksDataApiTests", DisableParallelization = true)]
[TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
public sealed class ProviderIntegrationTests : IDisposable {
	private readonly IConfiguration _configuration;
	private readonly IExchangeRateRepository _repo;
	private readonly ExchangeRatesDbContext _context;
	private readonly ILoggerFactory _loggerFactory;
	private readonly HttpClient _http;

	public ProviderIntegrationTests() {
		_configuration = GetConfiguration();
		_context = new ExchangeRatesDbContext(new DbContextOptionsBuilder<ExchangeRatesDbContext>()
			.UseSqlServer(_configuration.GetConnectionString("ExchangeRates")).Options);
		_loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		_repo = new ExchangeRateRepository(_context, _loggerFactory.CreateLogger<ExchangeRateRepository>());
		_http = new HttpClient();
	}

	public void Dispose() {
		_context.Dispose();
		_loggerFactory.Dispose();
		_http.Dispose();
	}

	private IConfiguration GetConfiguration() {
		return new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
															.AddJsonFile("appsettings.json", optional: false)
															.AddJsonFile("providerkeys.json", optional: false)
															.Build();
	}

	// Integration test for BOEProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BOE", CancellationToken.None);
		var provider = new BOEProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BOEProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("GBP", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BOE", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Tests the FRED provider to ensure it returns valid USD/BRL exchange rates for the specified date range.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdBrlRates_FromFRED_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("FRED", CancellationToken.None);

		Assert.NotNull(bank);
		Assert.NotNull(bank.NativeCurrency);
		Assert.Equal("USD", bank.NativeCurrency.CurrencyCode);

		var provider = new FREDProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<FREDProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("BRL", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("USD", rate.BaseCurrency);
			Assert.Equal("BRL", rate.QuoteCurrency);
			Assert.Equal("FRED", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BCBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_EurRates_FromBCB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BCB", CancellationToken.None);
		var provider = new BCBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BCBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("EUR", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("EUR", rate.BaseCurrency);
			Assert.Equal("BRL", rate.QuoteCurrency);
			Assert.Equal("BCB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BDIProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBDI_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BDI", CancellationToken.None);
		var provider = new BDIProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BDIProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("EUR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BDI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Tests the GetRatesAsync method of the BOIProvider class to ensure it returns valid ILS exchange rates from the Bank of Israel for a specified date range.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_IlsRates_FromBOI_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BOI", CancellationToken.None);
		var provider = new BOIProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BOIProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("ILS", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BOI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BOCProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_MxnRates_FromBOC_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BOC", CancellationToken.None);
		var provider = new BOCProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BOCProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("MXN", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("CAD", rate.BaseCurrency);
			Assert.Equal("MXN", rate.QuoteCurrency);
			Assert.Equal("BOC", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Tests the ECB provider to ensure it returns valid EUR/USD exchange rates for the specified date range.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_EurUsdRates_FromECB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("ECB", CancellationToken.None);

		Assert.NotNull(bank);
		Assert.NotNull(bank.NativeCurrency);
		Assert.Equal("EUR", bank.NativeCurrency.CurrencyCode);

		var provider = new ECBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<ECBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("EUR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("ECB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for SNBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_EurRates_FromSNB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("SNB", CancellationToken.None);
		var provider = new SNBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<SNBProvider>());
		var fromDate = new DateOnly(2026, 5, 1);
		var toDate = new DateOnly(2026, 7, 31);
		var rates = await provider.GetRatesAsync("EUR", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("CHF", rate.BaseCurrency);
			Assert.Equal("EUR", rate.QuoteCurrency);
			Assert.Equal("SNB", rate.Provider);
			Assert.True(rate.Rate > 0);
		});
	}

	// Integration test for RBAProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromRBA_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("RBA", CancellationToken.None);
		var provider = new RBAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<RBAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("AUD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("RBA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BCBOProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCBO_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BCBO", CancellationToken.None);
		var provider = new BCBOProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BCBOProvider>());
		var fromDate = new DateOnly(2026, 8, 20);
		var toDate = new DateOnly(2026, 8, 27);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("BOB", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BCBO", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCP_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BCP", CancellationToken.None);
		var provider = new BCPProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BCPProvider>());
		var fromDate = new DateOnly(2019, 8, 20);
		var toDate = new DateOnly(2019, 8, 27);
		// Act
		var rates = await provider.GetRatesAsync("DKK", fromDate, toDate, CancellationToken.None);
		//Assert
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("PYG", rate.BaseCurrency);
			Assert.Equal("DKK", rate.QuoteCurrency);
			Assert.Equal("BCP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromIMF_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("IMF", CancellationToken.None);
		var provider = new IMFProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<IMFProvider>());
		var fromDate = new DateOnly(2026, 8, 3);
		var toDate = new DateOnly(2026, 8, 7);

		// Act
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		// Assert
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("XDR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("IMF", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Tests the GetRatesAsync method of the CBUAEProvider class to ensure it returns valid AED exchange rates from the Central Bank of the UAE for a specified date range.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_AedRates_FromCBUAE_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBUAE", CancellationToken.None);
		var provider = new CBUAEProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBUAEProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("AED", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBUAE", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	/// <summary>
	/// ECBProvider integration test for "EUR" rates from "BCB" for a specific date range.
	/// European Central Bank
	/// </summary>

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBRB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBRB", CancellationToken.None);
		var provider = new NBRBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBRBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		// Act
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		// Assert
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("BYN", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBRB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBI_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BI", CancellationToken.None);
		var provider = new BIProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BIProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		// Act
		var rates = await provider.GetRatesAsync("AED", fromDate, toDate, CancellationToken.None);
		// Assert
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("IDR", rate.BaseCurrency);
			Assert.Equal("AED", rate.QuoteCurrency);
			Assert.Equal("BI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromAMCM_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("AMCM", CancellationToken.None);
		var provider = new AMCMProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<AMCMProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		// Act
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		// Assert
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("MOP", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("AMCM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBAM_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BAM", CancellationToken.None);
		var provider = new BAMProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BAMProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		// Act
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		// Assert
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("MAD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BAM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBANREP_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BANREP", CancellationToken.None);
		var provider = new BANREPProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BANREPProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		// Act
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		//  Assert
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("COP", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BANREP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Tests the GetRatesAsync method of the BANXICOProvider class to ensure it returns valid MXN exchange rates from BANXICO for a specified date range.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_MxnRates_FromBANXICO_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BANXICO", CancellationToken.None);
		var provider = new BANXICOProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BANXICOProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("MXN", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BANXICO", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BBKProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBBK_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BBK", CancellationToken.None);
		var provider = new BBKProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BBKProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("DEM", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BBK", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BCCProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCC_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BCC", CancellationToken.None);
		var provider = new BCCProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BCCProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("CUP", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BCC", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BCCRProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCCR_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BCCR", CancellationToken.None);
		var provider = new BCCRProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BCCRProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("CRC", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BCCR", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BCEAOProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCEAO_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BCEAO", CancellationToken.None);
		var provider = new BCEAOProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BCEAOProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("XOF", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BCEAO", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BCNProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCN_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BCN", CancellationToken.None);
		var provider = new BCNProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BCNProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("NIO", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BCN", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BCRAProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCRA_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BCRA", CancellationToken.None);
		var provider = new BCRAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BCRAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("ARS", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BCRA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BCTProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCT_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BCT", CancellationToken.None);
		var provider = new BCTProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BCTProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("TND", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BCT", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BCUProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCU_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BCU", CancellationToken.None);
		var provider = new BCUProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BCUProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("UYU", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BCU", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BDPProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBDP_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BDP", CancellationToken.None);
		var provider = new BDPProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BDPProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("PTE", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BDP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BNAProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBNA_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BNA", CancellationToken.None);
		var provider = new BNAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BNAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("AOA", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BNA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BNMProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBNM_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BNM", CancellationToken.None);
		var provider = new BNMProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BNMProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("MYR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BNM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BNRProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBNR_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BNR", CancellationToken.None);
		var provider = new BNRProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BNRProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("RON", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BNR", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BNRRWProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBNRRW_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BNRRW", CancellationToken.None);
		var provider = new BNRRWProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BNRRWProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("RWF", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BNRRW", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BOAProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOA_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BOA", CancellationToken.None);
		var provider = new BOAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BOAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("DZD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BOA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BOBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BOB", CancellationToken.None);
		var provider = new BOBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BOBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("BWP", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BOB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BOJAProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOJA_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BOJA", CancellationToken.None);
		var provider = new BOJAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BOJAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("JMD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BOJA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BOJProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOJ_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BOJ", CancellationToken.None);
		var provider = new BOJProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BOJProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("JPY", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BOJ", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BOMProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOM_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BOM", CancellationToken.None);
		var provider = new BOMProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BOMProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("MNT", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BOM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BOTAProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOTA_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BOTA", CancellationToken.None);
		var provider = new BOTAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BOTAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("TZS", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BOTA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BOTProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOT_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BOT", CancellationToken.None);
		var provider = new BOTProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BOTProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("THB", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BOT", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BRBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBRB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BRB", CancellationToken.None);
		var provider = new BRBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BRBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("BIF", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BRB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for BSPProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBSP_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("BSP", CancellationToken.None);
		var provider = new BSPProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<BSPProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("PHP", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("BSP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBAProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBA_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBA", CancellationToken.None);
		var provider = new CBAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("AMD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBCProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBC_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBC", CancellationToken.None);
		var provider = new CBCProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBCProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("TWD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBC", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBEProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBE_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBE", CancellationToken.None);
		var provider = new CBEProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBEProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("EGP", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBE", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBGProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBG_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBG", CancellationToken.None);
		var provider = new CBGProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBGProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("GMD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBG", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBIProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBI_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBI", CancellationToken.None);
		var provider = new CBIProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBIProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("IQD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBKProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBK_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBK", CancellationToken.None);
		var provider = new CBKProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBKProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("KES", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBK", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBLLRProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBLLR_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBLLR", CancellationToken.None);
		var provider = new CBLLRProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBLLRProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("LRD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBLLR", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBMProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBM_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBM", CancellationToken.None);
		var provider = new CBMProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBMProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("MMK", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBNProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBN_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBN", CancellationToken.None);
		var provider = new CBNProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBNProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("NGN", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBN", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBRProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBR_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBR", CancellationToken.None);
		var provider = new CBRProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBRProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("RUB", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBR", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBSLProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBSL_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBSL", CancellationToken.None);
		var provider = new CBSLProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBSLProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("LKR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBSL", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBSProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBS_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBS", CancellationToken.None);
		var provider = new CBSProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBSProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("WST", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBS", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CBUProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBU_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CBU", CancellationToken.None);
		var provider = new CBUProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CBUProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("UZS", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CBU", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for CNBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCNB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("CNB", CancellationToken.None);
		var provider = new CNBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<CNBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("CZK", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("CNB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for DABProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromDAB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("DAB", CancellationToken.None);
		var provider = new DABProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<DABProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("AFN", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("DAB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for DNBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromDNB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("DNB", CancellationToken.None);
		var provider = new DNBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<DNBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("DKK", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("DNB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Tests the GetRatesAsync method of the FBILProvider class to ensure it returns valid INR exchange rates from the Financial Benchmarks India for a specified date range.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_InrRates_FromFBIL_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("RBI", CancellationToken.None);
		var provider = new FBILProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<FBILProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("INR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("RBI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for HKMAProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromHKMA_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("HKMA", CancellationToken.None);
		var provider = new HKMAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<HKMAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("HKD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("HKMA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for HNBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromHNB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("HNB", CancellationToken.None);
		var provider = new HNBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<HNBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("EUR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("HNB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for LBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromLB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("LB", CancellationToken.None);
		var provider = new LBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<LBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("EUR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("LB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for MASProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromMAS_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("MAS", CancellationToken.None);
		var provider = new MASProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<MASProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("SGD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("MAS", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for MMAProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromMMA_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("MMA", CancellationToken.None);
		var provider = new MMAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<MMAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("MVR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("MMA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for MNBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromMNB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("MNB", CancellationToken.None);
		var provider = new MNBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<MNBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("HUF", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("MNB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBCProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBC_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBC", CancellationToken.None);
		var provider = new NBCProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBCProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("KHR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBC", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBEProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBE_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBE", CancellationToken.None);
		var provider = new NBEProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBEProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("ETB", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBE", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBGProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBG_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBG", CancellationToken.None);
		var provider = new NBGProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBGProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("GEL", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBG", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBKProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBK_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBK", CancellationToken.None);
		var provider = new NBKProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBKProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("KZT", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBK", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBKRProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBKR_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBKR", CancellationToken.None);
		var provider = new NBKRProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBKRProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("KGS", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBKR", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBMProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBM_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBM", CancellationToken.None);
		var provider = new NBMProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBMProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("MDL", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBPProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBP_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBP", CancellationToken.None);
		var provider = new NBPProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBPProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("PLN", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NB", CancellationToken.None);
		var provider = new NBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("NOK", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBRMProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBRM_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBRM", CancellationToken.None);
		var provider = new NBRMProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBRMProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("MKD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBRM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBTProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBT_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBT", CancellationToken.None);
		var provider = new NBTProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBTProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("TJS", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBT", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NBUProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBU_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NBU", CancellationToken.None);
		var provider = new NBUProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NBUProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("UAH", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NBU", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NRBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNRB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NRB", CancellationToken.None);
		var provider = new NRBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NRBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("NPR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NRB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for NRBTProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNRBT_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("NRBT", CancellationToken.None);
		var provider = new NRBTProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<NRBTProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("TOP", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("NRBT", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for RBFProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromRBF_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("RBF", CancellationToken.None);
		var provider = new RBFProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<RBFProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("FJD", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("RBF", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for RBMProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromRBM_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("RBM", CancellationToken.None);
		var provider = new RBMProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<RBMProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("MWK", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("RBM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for RBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromRB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("RB", CancellationToken.None);
		var provider = new RBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<RBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("SEK", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("RB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for RBVProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromRBV_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("RBV", CancellationToken.None);
		var provider = new RBVProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<RBVProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("VUV", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("RBV", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for SARBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromSARB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("SARB", CancellationToken.None);
		var provider = new SARBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<SARBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("ZAR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("SARB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for SBIProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromSBI_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("SBI", CancellationToken.None);
		var provider = new SBIProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<SBIProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("ISK", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("SBI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for SBPProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromSBP_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("SBP", CancellationToken.None);
		var provider = new SBPProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<SBPProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("PKR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("SBP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for TCMBProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromTCMB_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("TCMB", CancellationToken.None);
		var provider = new TCMBProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<TCMBProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("TRY", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("TCMB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Integration test for AFAProvider exchange-rate retrieval.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromAFA_ForDateRange() {
		var configuration = _configuration;
		var bank = await _repo.GetCentralBankAsync("AFA", CancellationToken.None);
		var provider = new AFAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<AFAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("USD", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("EUR", rate.BaseCurrency);
			Assert.Equal("USD", rate.QuoteCurrency);
			Assert.Equal("AFA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public void FixedExchangeRateProvider_ShouldReturn_AdpFixedRate() {
		var provider = new FixedExchangeRateProvider(_configuration);
		var found = provider.TryGetFixedRate("ADP", out var peggedOn, out var rate);
		Assert.True(found);
		Assert.Equal("EUR", peggedOn);
		Assert.Equal(6.55957m, rate);
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_TriangulatedRates_ForAdpToAed() {
		using var factory = new WebApplicationFactory<Program>();
		using var scope = factory.Services.CreateScope();
		var service = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
		var fromDate = new DateOnly(2026, 8, 3);
		var toDate = new DateOnly(2026, 8, 7);

		// "ADP" has no direct provider against "AED", so ExchangeRateService must triangulate
		// "ADP" (fixed) -> pivot ("USD") -> "AED" ("CBUAE") internally.
		var rates = await service.GetRatesAsync("ADP", "AED", fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("ADP", rate.BaseCurrency);
			Assert.Equal("AED", rate.QuoteCurrency);
			Assert.Contains('+', rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	// Tests the GetRatesAsync method of the SAMAProvider class to ensure it returns valid SAR exchange rates from the Saudi Central Bank for a specified date range.
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_SarRates_FromSAMA_ForDateRange() {
		var bank = await _repo.GetCentralBankAsync("SAMA", CancellationToken.None);
		var provider = new SAMAProvider(_http, _configuration, bank, _loggerFactory.CreateLogger<SAMAProvider>());
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync("EUR", fromDate, toDate, CancellationToken.None);

		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal("SAR", rate.BaseCurrency);
			Assert.Equal("EUR", rate.QuoteCurrency);
			Assert.Equal("SAMA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
}
