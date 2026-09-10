using ExchangeRates.Domain.Entities;
using ExchangeRates.Server.Extensions;
using ExchangeRates.Server.Interfaces;

using System.Globalization;
using System.Text.Json;

namespace ExchangeRates.Server.Providers;

/// <summary>
/// Base class for central bank exchange rate providers.
/// Provides shared HTTP, configuration, authentication, and utility functionality
/// for retrieving and parsing exchange rate data from central bank sources.
/// </summary>
public abstract class CentralBankProviderBase : ICentralBankProvider {
	protected CentralBankProviderBase(HttpClient http, IConfiguration configuration, CentralBankEntity bank) {
		Http = http;
		Configuration = configuration;
		Bank = bank;
	}

	protected HttpClient Http { get; }
	protected IConfiguration Configuration { get; }
	protected CentralBankEntity Bank { get; }
	public string BankCode => Bank.BankCode;
	public string NativeCurrencyCode => Bank.Currency.CurrencyCode;
	public string BankName => Bank.BankName;
	public string? CountryOfOrigin => Bank.CountryOfOrigin;
	public int? Priority => Bank.Priority;

	protected string Url => Configuration[$"CentralBanks:{Bank.BankCode}:Url"] ?? throw new InvalidOperationException($"Missing CentralBanks:{Bank.BankCode}:Url configuration.");

	protected string? HistoricalUrl => Configuration[$"CentralBanks:{Bank.BankCode}:HistoricalUrl"];

	protected string? ApiKey => Configuration[$"ProviderKeys:{Bank.BankCode}"];

	public string PivotCurrency { get => Configuration.GetSection($"CentralBanks:{Bank.BankCode}:Priority")?.Value?.Trim() ?? throw new InvalidOperationException($"Missing CentralBanks:{Bank.BankCode}:Priority configuration."); }
	public List<string> HistoricCurrencies { get => Configuration.GetSection($"CentralBanks:{Bank.BankCode}:HistoricCurrencies").Get<List<string>>() ?? throw new InvalidOperationException($"Missing CentralBanks:{Bank.BankCode}:HistoricCurrencies configuration."); }
	public IReadOnlySet<string> SupportedCurrencies {
		get {
			var currencies = Configuration.GetSection($"CentralBanks:{Bank.BankCode}:SupportedCurrencies").Get<string[]>() ?? throw new InvalidOperationException($"Missing CentralBanks:{Bank.BankCode}:SupportedCurrencies configuration.");
			return currencies.Select(x => x.ToECurrency()).ToHashSet();
		}
	}



	public bool Supports(string currency) {
		return currency == Bank.BankCode || SupportedCurrencies.Contains(currency);
	}

	/// <inheritdoc/>
	public Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		return FetchAsync(quoteCurrency, fromDate, toDate, ct);
	}

	/// <summary>
	/// Retrieves and parses exchange rate data directly from the central bank source.
	/// Provider-specific implementations should override this method.
	/// </summary>
	/// <param name="quoteCurrency"></param>
	/// <param name="fromDate"></param>
	/// <param name="toDate"></param>
	protected abstract Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);

	protected static decimal GetDecimal(JsonElement element, string propertyName) {
		if (!element.TryGetProperty(propertyName, out var property)) {
			return 0m;
		}

		if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var number)) {
			return number;
		}

		if (property.ValueKind == JsonValueKind.String && decimal.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) {
			return parsed;
		}
		return 0m;
	}
}

