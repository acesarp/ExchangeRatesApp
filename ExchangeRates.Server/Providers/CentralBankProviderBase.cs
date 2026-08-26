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
	public abstract string Name { get; }
	public abstract ECurrencyISO NativeCurrency { get; }
	public IReadOnlySet<ECurrencyISO> SupportedCurrencies {
		get {
			var currencies = Configuration.GetSection($"CentralBanks:{Code}:SupportedCurrencies").Get<string[]>() ?? [];
			return currencies.Select(x => x.ToECurrency()).ToHashSet();
		}
	}

	public bool Supports(ECurrencyISO currency) {
		return currency == NativeCurrency || SupportedCurrencies.Contains(currency);
	}

	/// <inheritdoc/>
	public Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(ECurrencyISO currency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		return FetchAsync(currency, fromDate, toDate, ct);
	}

	/// <summary>
	/// Retrieves and parses exchange rate data directly from the central bank source.
	/// Provider-specific implementations should override this method.
	/// </summary>
	/// <param name="fromDate"></param>
	/// <param name="ct"></param>
	/// <returns>IReadOnlyList&lt;ExchangeRate&gt;</returns>
	protected virtual Task<IReadOnlyList<ExchangeRate>> FetchAsync(ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		throw new NotSupportedException($"{Code} ({Name}) is registered, needs a bank-specific parser/endpoint implementation.");
	}

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

