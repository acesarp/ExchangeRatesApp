using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;
using ExchangeRates.Server.Tests.Fixtures;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace ExchangeRates.Server.Tests.Unit;

public class CentralBankProviderFactoryTests {
	[Fact]
	public void GetAll_WithRegisteredProviders_ReturnsAll() {
		// Arrange
		var services = new ServiceCollection()
			.AddLogging(builder => builder.AddConsole());

		var provider1 = MockDataBuilder.CreateTestProvider("ECB", ECurrencyISO.EUR);
		var provider2 = MockDataBuilder.CreateTestProvider("FED", ECurrencyISO.USD);

		services.AddSingleton<ICentralBankProvider>(provider1);
		services.AddSingleton<ICentralBankProvider>(provider2);

		var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<CentralBankProviderFactory>>();
		var configuration = serviceProvider.GetRequiredService<IConfiguration>();
		var factory = new CentralBankProviderFactory(configuration, serviceProvider.GetServices<ICentralBankProvider>(), logger);

		// Act
		var result = factory.GetAllProviders();

		// Assert
		result.Should().HaveCountGreaterThanOrEqualTo(2);
	}

	[Fact]
	public void GetAll_NoProviders_ReturnsEmpty() {
		// Arrange
		var services = new ServiceCollection()
			.AddLogging(builder => builder.AddConsole());

		var serviceProvider = services.BuildServiceProvider();
		var configuration = serviceProvider.GetRequiredService<IConfiguration>();
		var logger = serviceProvider.GetRequiredService<ILogger<CentralBankProviderFactory>>();
		var factory = new CentralBankProviderFactory(configuration, [], logger);

		// Act
		var result = factory.GetAllProviders();

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public void Get_WithNonExistentCode_Throws() {
		// Arrange
		var services = new ServiceCollection()
			.AddLogging(builder => builder.AddConsole());

		var serviceProvider = services.BuildServiceProvider();
		var configuration = serviceProvider.GetRequiredService<IConfiguration>();
		var logger = serviceProvider.GetRequiredService<ILogger<CentralBankProviderFactory>>();
		var factory = new CentralBankProviderFactory(configuration, [], logger);

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() => factory.GetProvider("NONEXISTENT"));
	}

	[Fact]
	public void Get_WithInvalidCode_ThrowsArgumentOutOfRangeException() {
		// Arrange
		var services = new ServiceCollection()
			.AddLogging(builder => builder.AddConsole());

		var serviceProvider = services.BuildServiceProvider();
		var configuration = serviceProvider.GetRequiredService<IConfiguration>();
		var logger = serviceProvider.GetRequiredService<ILogger<CentralBankProviderFactory>>();
		var factory = new CentralBankProviderFactory(configuration, [], logger);

		// Act & Assert
		Assert.Throws<ArgumentOutOfRangeException>(() => factory.GetProvider("INVALID"));
	}

	[Fact]
	public void GetAll_WithRegisteredTesting() {
		// Arrange
		var services = new ServiceCollection()
			.AddLogging(builder => builder.AddConsole());

		var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<CentralBankProviderFactory>>();
		// Act & Assert
		var provider = MockDataBuilder.CreateTestProvider("TEST", ECurrencyISO.USD);
		provider.Supports(ECurrencyISO.USD).Should().BeTrue();
		provider.Supports(ECurrencyISO.EUR).Should().BeTrue();
	}

	[Fact]
	public void Get_WithRegisteredProvider_ReturnsProviderWithoutConcreteRegistration() {
		// Arrange
		var provider = MockDataBuilder.CreateTestProvider("ECB", ECurrencyISO.EUR);
		var services = new ServiceCollection()
			.AddLogging()
			.AddSingleton<ICentralBankProvider>(provider)
			.AddTransient<CentralBankProviderFactory>();
		var serviceProvider = services.BuildServiceProvider();
		var factory = serviceProvider.GetRequiredService<CentralBankProviderFactory>();

		// Act
		var result = factory.GetProvider("ecb");

		// Assert
		result.Should().BeSameAs(provider);
	}
}
