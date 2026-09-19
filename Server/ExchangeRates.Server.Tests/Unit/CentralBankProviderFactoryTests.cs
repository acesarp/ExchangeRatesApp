using ExchangeRates.Domain.Entities;
using ExchangeRates.Server.Providers;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace ExchangeRates.Server.Tests.Unit;

public class CentralBankProviderFactoryTests {

	[Fact]
	public void GetProvider_WithUnknownBankCode_ThrowsArgumentOutOfRangeException() {
		// Arrange
		using var serviceProvider = CreateServiceProvider();
		var factory = CreateFactory(serviceProvider);
		var bank = CreateBank("INVALID", "USD");

		// Act
		var act = () => factory.GetProvider(bank);

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>()
			.WithMessage("*Unknown central bank provider 'INVALID'*");
	}

	[Fact]
	public void GetProvider_WithRegisteredBankCode_ReturnsCorrectProvider() {
		// Arrange
		using var serviceProvider = CreateServiceProvider();
		var factory = CreateFactory(serviceProvider);
		var bank = CreateBank("ECB", "EUR");

		// Act
		var result = factory.GetProvider(bank);

		// Assert
		result.Should().BeOfType<ECBProvider>();
	}

	[Fact]
	public void GetProvider_WithLowerCaseBankCode_IsCaseInsensitive() {
		// Arrange
		using var serviceProvider = CreateServiceProvider();
		var factory = CreateFactory(serviceProvider);
		var bank = CreateBank("ecb", "EUR");

		// Act
		var result = factory.GetProvider(bank);

		// Assert
		result.Should().BeOfType<ECBProvider>();
	}

	[Fact]
	public void GetProvider_CalledTwice_ReturnsDifferentInstances() {
		// Arrange
		using var serviceProvider = CreateServiceProvider();
		var factory = CreateFactory(serviceProvider);
		var bank = CreateBank("ECB", "EUR");

		// Act
		var provider1 = factory.GetProvider(bank);
		var provider2 = factory.GetProvider(bank);

		// Assert
		provider1.Should().NotBeSameAs(provider2);
	}

	private static ServiceProvider CreateServiceProvider() {
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> {
				["CentralBanks:ECB:ApiUrl"] = "https://example.com"
			})
			.Build();

		var services = new ServiceCollection();

		services.AddLogging();
		services.AddSingleton<IConfiguration>(configuration);
		services.AddHttpClient();

		return services.BuildServiceProvider();
	}

	private static CentralBankProviderFactory CreateFactory(IServiceProvider services) {
		var logger = services.GetRequiredService<ILogger<CentralBankProviderFactory>>();

		return new CentralBankProviderFactory(services, logger);
	}

	private static CentralBankEntity CreateBank(string bankCode, string currencyCode) {
		var currency = new CurrencyEntity(currencyCode, 978, currencyCode, false, 1);

		return new CentralBankEntity(
			bankCode,
			$"{bankCode} Test Bank",
			"Test Country",
			currency.Id,
			true,
			DateTime.UtcNow) {
			NativeCurrency = currency
		};
	}
}