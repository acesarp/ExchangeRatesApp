using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;

using System.Globalization;
using System.Text.Json;

namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Base class for central bank exchange rate providers.
/// Provides shared HTTP, configuration, authentication, and utility functionality
/// for retrieving and parsing exchange rate data from central bank sources.
/// </summary>
public abstract class CentralBankProviderBase : ICentralBankProvider {
	private readonly Lazy<IReadOnlySet<string>> _supportedCurrencies;
	private readonly Lazy<List<string>> _historicCurrencies;
	private readonly Lazy<string> _pivotCurrency;

	protected CentralBankProviderBase(HttpClient http, CentralBankEntity bank, IConfiguration configuration) {
		Http = http;
		Configuration = configuration;
		Bank = bank;

		// Initialize lazy-loaded configuration properties to avoid repeated GetSection calls
		_supportedCurrencies = new Lazy<IReadOnlySet<string>>(() => Bank.SupportedCurrencies.Select(c => c.Currency.CurrencyCode).ToHashSet());

		_historicCurrencies = new Lazy<List<string>>(() => Bank.SupportedCurrencies.Where(c => c.Currency.IsHistoric)
																					.Select(c => c.Currency.CurrencyCode)
																					.ToList()
																				);

		_pivotCurrency = new Lazy<string>(() =>
			Configuration.GetSection($"CentralBanks:{Bank.BankCode}:Priority")?.Value?.Trim()
			?? throw new InvalidOperationException($"Missing CentralBanks:{Bank.BankCode}:Priority configuration.")
		);
	}

	protected HttpClient Http { get; }
	protected IConfiguration Configuration { get; }
	protected CentralBankEntity Bank { get; }
	public string BankCode => Bank.BankCode;
	public string NativeCurrencyCode => Bank.NativeCurrency.CurrencyCode;
	public string BankName => Bank.BankName;
	public string? CountryOfOrigin => Bank.CountryOfOrigin;
	public int? Priority => Bank.Priority;

	protected string ApiUrl => Bank.ApiUrl ?? throw new InvalidOperationException($"Central bank {Bank.BankCode} has no ApiUrl configured.");

	protected string? HistoricalUrl => Configuration[$"CentralBanks:{Bank.BankCode}:HistoricalUrl"];

	protected string? ApiKey => Configuration[$"ProviderKeys:{Bank.BankCode}"];

	public string PivotCurrency => _pivotCurrency.Value;
	public List<string> HistoricCurrencies => _historicCurrencies.Value;
	public IReadOnlySet<string> SupportedCurrencies => _supportedCurrencies.Value;
	public IReadOnlySet<ProviderSeries>? Series => Bank.Series;

	public bool Supports(string currency) {
		return currency == Bank.BankCode || SupportedCurrencies.Contains(currency);
	}

	/// <summary>
	/// Returns rates as "1 unit of the native currency = Rate units of the quote currency" (e.g. ECB: 1 EUR = 1.12 USD),
	/// regardless of how the source publishes them.
	/// </summary>
	public async Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var rates = await FetchAsync(quoteCurrency, fromDate, toDate, ct);

		if (Quotation == RateQuotation.ForeignPerNative) {
			return rates;
		}

		return rates.Where(x => x.Rate != 0)
					.Select(x => x with { Rate = 1m / x.Rate })
					.ToList();
	}

	/// <summary>
	/// How the values returned by <see cref="FetchAsync"/> are quoted. Every provider must declare it so that
	/// <see cref="GetRatesAsync"/> can normalize the direction.
	/// </summary>
	protected abstract RateQuotation Quotation { get; }

	/// <summary>
	/// Retrieves and parses exchange rate data directly from the central bank source, quoted as declared by <see cref="Quotation"/>.
	/// Provider-specific implementations should override this method.
	/// </summary>
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

