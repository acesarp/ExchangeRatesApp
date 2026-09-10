using ExchangeRates.Domain.Entities;
using ExchangeRates.Infrastructure;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

using Xunit;
using Xunit.Abstractions;

namespace ExchangeRates.Server.Tests.Integration;

public sealed class ProviderCoverageIntegrationTests {
	private readonly ITestOutputHelper _output;

	public ProviderCoverageIntegrationTests(ITestOutputHelper output) {
		_output = output;
	}

	[Fact]
	public async Task AllCurrentCurrencies_ShouldHaveProvider_WhenBaseCurrencyIsUSD() {
		var missingCurrentCurrencies = await GetMissingProviderCurrenciesAsync(isHistoric: false);

		WriteMissingCurrencies("Current currencies", missingCurrentCurrencies);

		Assert.True(
			missingCurrentCurrencies.Count == 0,
			$"No provider found for {missingCurrentCurrencies.Count} current currencies: {string.Join(", ", missingCurrentCurrencies)}");
	}

	[Fact]
	public async Task AllHistoricCurrencies_ShouldHaveProvider_WhenBaseCurrencyIsUSD() {
		var missingHistoricCurrencies = await GetMissingProviderCurrenciesAsync(isHistoric: true);

		WriteMissingCurrencies("Historic currencies", missingHistoricCurrencies);

		Assert.True(
			missingHistoricCurrencies.Count == 0,
			$"No provider found for {missingHistoricCurrencies.Count} historic currencies: {string.Join(", ", missingHistoricCurrencies)}");
	}

	private async Task<List<string>> GetMissingProviderCurrenciesAsync(bool isHistoric) {
		using var factory = new WebApplicationFactory<Program>();
		using var scope = factory.Services.CreateScope();

		var dbContext = scope.ServiceProvider.GetRequiredService<ExchangeRatesDbContext>();
		var service = scope.ServiceProvider.GetRequiredService<IExchangeRateService>() as ExchangeRateService;

		Assert.NotNull(service);

		var findProviderMethod = typeof(ExchangeRateService).GetMethod(
			"FindProvider",
			BindingFlags.Instance | BindingFlags.NonPublic);

		Assert.NotNull(findProviderMethod);

		var currencies = await dbContext.Currencies
			.AsNoTracking()
			.Where(c => c.IsHistoric == isHistoric)
			.OrderBy(c => c.CurrencyCode)
			.ToListAsync();

		var missingProviders = new List<string>();

		foreach (CurrencyEntity currency in currencies) {
			if (string.Equals(currency.CurrencyCode, "USD", StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			if (string.IsNullOrEmpty(currency.CurrencyCode)) {
				missingProviders.Add($"{currency.CurrencyCode} - {currency.Name}");
				continue;
			}

			var provider = (ICentralBankProvider?)findProviderMethod.Invoke(service, new object[] { "USD", currency.CurrencyCode });
			if (provider is null) {
				missingProviders.Add($"{currency.CurrencyCode} - {currency.Name}");
			}
		}

		return missingProviders;
	}

	private void WriteMissingCurrencies(string groupName, IReadOnlyCollection<string> missingCurrencies) {
		_output.WriteLine($"{groupName} without provider: {missingCurrencies.Count}");
		foreach (string item in missingCurrencies) {
			_output.WriteLine(item);
		}
	}
}
