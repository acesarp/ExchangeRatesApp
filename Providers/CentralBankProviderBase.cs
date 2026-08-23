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

	protected string Url => Configuration[$"CentralBanks:{Code}:Url"]
		?? throw new InvalidOperationException($"Missing CentralBanks:{Code}:Url configuration.");

	protected string? HistoricalUrl => Configuration[$"CentralBanks:{Code}:HistoricalUrl"];

	protected string? ApiKey => Configuration[$"ProviderKeys:{Code}"];

	public abstract string Code { get; }
	public abstract string Name { get; }
	public abstract string NativeCurrency { get; }

	public Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(DateOnly? date, CancellationToken ct) {
		return FetchAsync(date ?? DateOnly.FromDateTime(DateTime.UtcNow), ct);
	}
	protected virtual Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken ct) {
		throw new NotSupportedException($"{Code} ({Name}) is registered, but its direct-source adapter still needs a bank-specific parser/endpoint implementation.");
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

