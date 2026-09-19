using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Infrastructure;
using ExchangeRates.Infrastructure.Repositories;
using ExchangeRates.Server.Providers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Xunit;

namespace ExchangeRates.Server.Tests.Integration;

[Collection("CentralBanksDataApiTests")]
[Trait("Category", "Integration")]
[CollectionDefinition("CentralBanksDataApiTests", DisableParallelization = true)]
[TestCaseOrderer("Xunit.Extensions.Ordering.TestCaseOrderer", "Xunit.Extensions.Ordering")]
public sealed class CentralBanksDataApiTests : IDisposable {
	private readonly IConfiguration _configuration;
	private readonly IExchangeRateRepository _repo;
	private readonly ExchangeRatesDbContext _context;
	private readonly ILoggerFactory _loggerFactory;
	private readonly HttpClient _http;
	public CentralBanksDataApiTests() {
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

	// Tests the FRED provider to ensure it returns valid USD/BR exchange rates for the specified date range
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

	// Tests the ECB provider to ensure it returns valid EUR/USD exchange rates for the specified date range
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
}
