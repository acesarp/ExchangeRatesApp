using ExchangeRates.Domain.Enums;
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
	protected CentralBankProviderBase(HttpClient http, IConfiguration configuration) {
		Http = http;
		Configuration = configuration;
	}

	protected HttpClient Http { get; }
	protected IConfiguration Configuration { get; }

	protected string Url => Configuration[$"CentralBanks:{Code}:Url"] ?? throw new InvalidOperationException($"Missing CentralBanks:{Code}:Url configuration.");

	protected string? HistoricalUrl => Configuration[$"CentralBanks:{Code}:HistoricalUrl"];

	protected string? ApiKey => Configuration[$"ProviderKeys:{Code}"];

	public abstract string Code { get; }
	public ECurrencyISO PivotCurrency { get => Configuration.GetSection($"CentralBanks:{Code}:PivotCurrency")?.Value?.Trim().ToECurrency() ?? throw new InvalidOperationException($"Missing CentralBanks:{Code}:PivotCurrency configuration."); }
	public bool InverseProvider { get; set; }
	public string BankName { get => Configuration.GetSection($"CentralBanks:{Code}:BankName")?.Value?.Trim() ?? throw new InvalidOperationException($"Missing CentralBanks:{Code}:BankName configuration."); }
	public ECurrencyISO NativeCurrency { get => Configuration.GetSection($"CentralBanks:{Code}:NativeCurrency")?.Value?.Trim().ToECurrency() ?? throw new InvalidOperationException($"Missing CentralBanks:{Code}:NativeCurrency configuration."); }
	public string CountryOfOrigin { get => Configuration.GetSection($"CentralBanks:{Code}:CountryOfOrigin")?.Value?.Trim() ?? throw new InvalidOperationException($"Missing CentralBanks:{Code}:CountryOfOrigin configuration."); }
	public List<string> HistoricCurrencies { get => Configuration.GetSection($"CentralBanks:{Code}:HistoricCurrencies").Get<List<string>>() ?? throw new InvalidOperationException($"Missing CentralBanks:{Code}:HistoricCurrencies configuration."); }
	public IReadOnlySet<ECurrencyISO> SupportedCurrencies {
		get {
			var currencies = Configuration.GetSection($"CentralBanks:{Code}:SupportedCurrencies").Get<string[]>() ?? throw new InvalidOperationException($"Missing CentralBanks:{Code}:SupportedCurrencies configuration.");
			return currencies.Select(x => x.ToECurrency()).ToHashSet();
		}
	}

	public bool Supports(ECurrencyISO currency) {
		return currency == NativeCurrency || SupportedCurrencies.Contains(currency);
	}

	/// <inheritdoc/>
	public Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(ECurrencyISO currency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		return FetchAsync(currency, fromDate, toDate, ct);
	}

	/// <summary>
	/// Retrieves and parses exchange rate data directly from the central bank source.
	/// Provider-specific implementations should override this method.
	/// </summary>
	/// <param name="fromDate"></param>
	/// <param name="ct"></param>
	/// <returns>IReadOnlyList&lt;ExchangeRate&gt;</returns>
	protected abstract Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);

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

