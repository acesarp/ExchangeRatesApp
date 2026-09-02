using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.ExchangeRates;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;
namespace ExchangeRates.Server.Tests.Integration;

/// <summary>
/// Integration tests for the exchange rate providers.
/// </summary>
public sealed class ProviderIntegrationTests {
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOEProvider>();
		var provider = new BOEProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.GBP, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BOE", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_EurRates_FromFRED_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<FREDProvider>();
		var provider = new FREDProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.EUR, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.USD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.EUR, rate.QuoteCurrency);
			Assert.Equal("FRED", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_EurRates_FromBCB_ForDateRange() {
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCBProvider>();
		using var http = new HttpClient();
		var provider = new BCBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.EUR, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EUR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.BRL, rate.QuoteCurrency);
			Assert.Equal("BCB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBDI_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BDIProvider>();
		var provider = new BDIProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EUR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BDI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_BrlRates_FromBOI_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOIProvider>();
		var provider = new BOIProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.BRL, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.ILS, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.BRL, rate.QuoteCurrency);
			Assert.Equal("BOI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_MxnRates_FromBOC_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOCProvider>();
		var provider = new BOCProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.MXN, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.CAD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.MXN, rate.QuoteCurrency);
			Assert.Equal("BOC", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_ThbRates_FromECB_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<ECBProvider>();
		var provider = new ECBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.THB, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EUR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.THB, rate.QuoteCurrency);
			Assert.Equal("ECB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_ChfRates_FromECB_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<ECBProvider>();
		var provider = new ECBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.CHF, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EUR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.CHF, rate.QuoteCurrency);
			Assert.Equal("ECB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_EurRates_FromSNB_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<SNBProvider>();
		var provider = new SNBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 5, 1);
		var toDate = new DateOnly(2026, 7, 31);
		var rates = await provider.GetRatesAsync(ECurrencyISO.EUR, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.CHF, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.EUR, rate.QuoteCurrency);
			Assert.Equal("SNB", rate.Provider);
			Assert.True(rate.Rate > 0);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromRBA_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<RBAProvider>();
		var provider = new RBAProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.AUD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("RBA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCBO_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCBOProvider>();
		var provider = new BCBOProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 20);
		var toDate = new DateOnly(2026, 8, 27);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.BOB, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BCBO", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCP_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCPProvider>();
		var provider = new BCPProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2019, 8, 20);
		var toDate = new DateOnly(2019, 8, 27);
		var rates = await provider.GetRatesAsync(ECurrencyISO.DKK, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.PYG, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.DKK, rate.QuoteCurrency);
			Assert.Equal("BCP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromIMF_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<IMFProvider>();
		var provider = new IMFProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 3);
		var toDate = new DateOnly(2026, 8, 7);

		// Act
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);

		// Assert
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.XDR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("IMF", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRates_AED_USD_ShouldReturnCBUAERates() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBUAEProvider>();
		var provider = new CBUAEProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 3);
		var toDate = new DateOnly(2026, 8, 7);
		// Act
		var rates = await provider.GetRatesAsync(ECurrencyISO.BRL, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.AED, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.BRL, rate.QuoteCurrency);
			Assert.Equal("CBUAE", rate.Provider);
			Assert.True(rate.Rate > 0);
		});
	}

	/// <summary>
	/// ECBProvider integration test for EUR rates from BCB for a specific date range.
	/// European Central Bank
	/// </summary>
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_BRLRates_FromECB_ForDateRange() {


		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<ECBProvider>();
		using var http = new HttpClient();
		var provider = new ECBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.BRL, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EUR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.BRL, rate.QuoteCurrency);
			Assert.Equal("ECB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBRB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBRBProvider>();
		var provider = new NBRBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.BYN, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBRB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBI_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BIProvider>();
		var provider = new BIProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.AED, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.IDR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.AED, rate.QuoteCurrency);
			Assert.Equal("BI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromAMCM_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<AMCMProvider>();
		var provider = new AMCMProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.MOP, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("AMCM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBAM_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BAMProvider>();
		var provider = new BAMProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.MAD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BAM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBANREP_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BANREPProvider>();
		var provider = new BANREPProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.COP, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BANREP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBANXICO_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BANXICOProvider>();
		var provider = new BANXICOProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.MXN, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BANXICO", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBBK_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BBKProvider>();
		var provider = new BBKProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.DEM, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BBK", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCC_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCCProvider>();
		var provider = new BCCProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.CUP, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BCC", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCCR_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCCRProvider>();
		var provider = new BCCRProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.CRC, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BCCR", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCEAO_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCEAOProvider>();
		var provider = new BCEAOProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.XOF, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BCEAO", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCN_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCNProvider>();
		var provider = new BCNProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.NIO, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BCN", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCRA_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCRAProvider>();
		var provider = new BCRAProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.ARS, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BCRA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCT_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCTProvider>();
		var provider = new BCTProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.TND, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BCT", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBCU_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BCUProvider>();
		var provider = new BCUProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.UYU, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BCU", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBDP_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BDPProvider>();
		var provider = new BDPProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.PTE, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BDP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBNA_ForDateRange() {

		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BNAProvider>();
		var provider = new BNAProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.AOA, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BNA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBNM_ForDateRange() {

		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BNMProvider>();
		var provider = new BNMProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.MYR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BNM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBNR_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BNRProvider>();
		var provider = new BNRProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.RON, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BNR", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBNRRW_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BNRRWProvider>();
		var provider = new BNRRWProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.RWF, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BNRRW", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOA_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOAProvider>();
		var provider = new BOAProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.DZD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BOA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOBProvider>();
		var provider = new BOBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.BWP, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BOB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOJA_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOJAProvider>();
		var provider = new BOJAProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.JMD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BOJA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOJ_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOJProvider>();
		var provider = new BOJProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.JPY, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BOJ", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOM_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOMProvider>();
		var provider = new BOMProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.MNT, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BOM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOTA_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOTAProvider>();
		var provider = new BOTAProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.TZS, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BOTA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBOT_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BOTProvider>();
		var provider = new BOTProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.THB, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BOT", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBRB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BRBProvider>();
		var provider = new BRBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.BIF, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BRB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromBSP_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<BSPProvider>();
		var provider = new BSPProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.PHP, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("BSP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBA_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBAProvider>();
		var provider = new CBAProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.AMD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBC_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBCProvider>();
		var provider = new CBCProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.TWD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBC", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBE_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBEProvider>();
		var provider = new CBEProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EGP, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBE", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBG_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBGProvider>();
		var provider = new CBGProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.GMD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBG", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBI_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBIProvider>();
		var provider = new CBIProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.IQD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBK_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBKProvider>();
		var provider = new CBKProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.KES, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBK", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBLLR_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBLLRProvider>();
		var provider = new CBLLRProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.LRD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBLLR", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBM_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBMProvider>();
		var provider = new CBMProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.MMK, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBN_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBNProvider>();
		var provider = new CBNProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.NGN, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBN", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBR_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBRProvider>();
		var provider = new CBRProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.RUB, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBR", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBSL_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBSLProvider>();
		var provider = new CBSLProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.LKR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBSL", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBS_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBSProvider>();
		var provider = new CBSProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.WST, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBS", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCBU_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CBUProvider>();
		var provider = new CBUProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.UZS, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CBU", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromCNB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<CNBProvider>();
		var provider = new CNBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.CZK, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("CNB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromDAB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<DABProvider>();
		var provider = new DABProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.AFN, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("DAB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromDNB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<DNBProvider>();
		var provider = new DNBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.DKK, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("DNB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromFBIL_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<FBILProvider>();
		var provider = new FBILProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.INR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("FBIL", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromHKMA_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<HKMAProvider>();
		var provider = new HKMAProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.HKD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("HKMA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromHNB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<HNBProvider>();
		var provider = new HNBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EUR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("HNB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromLB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<LBProvider>();
		var provider = new LBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EUR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("LB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromMAS_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<MASProvider>();
		var provider = new MASProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.SGD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("MAS", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromMMA_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<MMAProvider>();
		var provider = new MMAProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.MVR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("MMA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromMNB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<MNBProvider>();
		var provider = new MNBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.HUF, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("MNB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBC_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBCProvider>();
		var provider = new NBCProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.KHR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBC", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBE_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBEProvider>();
		var provider = new NBEProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.ETB, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBE", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBG_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBGProvider>();
		var provider = new NBGProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.GEL, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBG", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBK_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBKProvider>();
		var provider = new NBKProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.KZT, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBK", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBKR_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBKRProvider>();
		var provider = new NBKRProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.KGS, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBKR", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBM_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBMProvider>();
		var provider = new NBMProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.MDL, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBP_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBPProvider>();
		var provider = new NBPProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.PLN, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBProvider>();
		var provider = new NBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.NOK, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBRM_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBRMProvider>();
		var provider = new NBRMProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.MKD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBRM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBT_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBTProvider>();
		var provider = new NBTProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.TJS, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBT", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNBU_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NBUProvider>();
		var provider = new NBUProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.UAH, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NBU", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNRB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NRBProvider>();
		var provider = new NRBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.NPR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NRB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromNRBT_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<NRBTProvider>();
		var provider = new NRBTProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.TOP, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("NRBT", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromRBF_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<RBFProvider>();
		var provider = new RBFProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.FJD, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("RBF", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromRBM_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<RBMProvider>();
		var provider = new RBMProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.MWK, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("RBM", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromRB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<RBProvider>();
		var provider = new RBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.SEK, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("RB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromRBV_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<RBVProvider>();
		var provider = new RBVProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.VUV, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("RBV", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromSARB_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<SARBProvider>();
		var provider = new SARBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.ZAR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("SARB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromSBI_ForDateRange() {


		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<SBIProvider>();
		var provider = new SBIProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.ISK, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("SBI", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromSBP_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<SBPProvider>();
		var provider = new SBPProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.PKR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("SBP", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromTCMB_ForDateRange() {
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<TCMBProvider>();
		var provider = new TCMBProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.TRY, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("TCMB", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_UsdRates_FromAFA_ForDateRange() {
		var configuration = GetConfiguration();
		using var http = new HttpClient();
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<AFAProvider>();
		var provider = new AFAProvider(http, GetConfiguration(), logger);
		var fromDate = new DateOnly(2026, 8, 10);
		var toDate = new DateOnly(2026, 8, 14);
		var rates = await provider.GetRatesAsync(ECurrencyISO.USD, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.EUR, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.USD, rate.QuoteCurrency);
			Assert.Equal("AFA", rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}
	[Fact]
	public void FixedExchangeRateProvider_ShouldReturn_AdpFixedRate() {
		var provider = new FixedExchangeRateProvider(GetConfiguration());
		var found = provider.TryGetFixedRate(ECurrencyISO.ADP, out var rate);
		Assert.True(found);
		Assert.Equal(6.55957m, rate);
	}
	[Fact]
	public async Task GetRatesAsync_ShouldReturn_TriangulatedRates_ForAdpToAed() {
		using var factory = new WebApplicationFactory<Program>();
		using var scope = factory.Services.CreateScope();
		var service = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
		var fromDate = new DateOnly(2026, 8, 3);
		var toDate = new DateOnly(2026, 8, 7);

		// ADP has no direct provider against AED, so ExchangeRateService must triangulate
		// ADP (fixed) -> pivot (USD) -> AED (CBUAE) internally.
		var rates = await service.GetRatesAsync(ECurrencyISO.ADP, ECurrencyISO.AED, fromDate, toDate, CancellationToken.None);
		Assert.NotNull(rates);
		Assert.NotEmpty(rates);
		Assert.All(rates, rate => {
			Assert.Equal(ECurrencyISO.ADP, rate.BaseCurrency);
			Assert.Equal(ECurrencyISO.AED, rate.QuoteCurrency);
			Assert.Contains('+', rate.Provider);
			Assert.True(rate.Rate > 0);
			Assert.InRange(rate.Date, fromDate, toDate);
		});
	}

	[Theory]
	[InlineData(ECurrencyISO.AED, ECurrencyISO.USD, 3.6725)]
	[InlineData(ECurrencyISO.BAM, ECurrencyISO.EUR, 1.95583)]
	[InlineData(ECurrencyISO.BND, ECurrencyISO.SGD, 1.0)]
	[InlineData(ECurrencyISO.BTN, ECurrencyISO.INR, 1.0)]
	[InlineData(ECurrencyISO.NPR, ECurrencyISO.INR, 1.6)]
	[InlineData(ECurrencyISO.ADP, ECurrencyISO.EUR, 166.386)]
	public void TryGetRate_ConfiguredCurrency_ReturnsExpectedRate(ECurrencyISO currency, ECurrencyISO expectedPeggedOn, double expectedRate) {
		var fixedExchangeRates = new FixedExchangeRates(GetConfiguration());
		var success = fixedExchangeRates.TryGetRate(currency, out var peggedOn, out var rate);
		Assert.True(success);
		Assert.Equal(expectedPeggedOn, peggedOn);
		Assert.Equal((decimal)expectedRate, rate);
	}
	[Fact]
	public void TryGetRate_NonFixedCurrency_ReturnsFalse() {
		var fixedExchangeRates = new FixedExchangeRates(GetConfiguration());
		var success = fixedExchangeRates.TryGetRate(ECurrencyISO.CAD, out var peggedOn, out var rate);
		Assert.False(success);
		Assert.Equal(default, peggedOn);
		Assert.Equal(0m, rate);
	}

	private IConfiguration GetConfiguration() {
		return new ConfigurationBuilder()
					.SetBasePath(AppContext.BaseDirectory)
					.AddJsonFile("appsettings.json", optional: false)
					.AddJsonFile("providerkeys.json", optional: false)
					.Build();

	}
}
