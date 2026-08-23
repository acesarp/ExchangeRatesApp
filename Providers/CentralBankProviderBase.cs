using System.Globalization;
using System.Text.Json;

namespace ExchangeRates.Server.Providers;

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

	protected string? ApiKey => Configuration[$"CentralBanks:{Code}:ApiKey"];

	public abstract string Code { get; }
	public abstract string Name { get; }
	public abstract string NativeCurrency { get; }

	public Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(DateOnly? date = null, CancellationToken cancellationToken = default)
		=> FetchAsync(date ?? DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);

	protected virtual Task<IReadOnlyList<ExchangeRate>> FetchAsync(DateOnly date, CancellationToken cancellationToken)
		=> throw new NotSupportedException($"{Code} ({Name}) is registered, but its direct-source adapter still needs a bank-specific parser/endpoint implementation.");

	protected static decimal GetDecimal(JsonElement element, string propertyName) {
		if (!element.TryGetProperty(propertyName, out var property)) {
			return 0m;
		}

		if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var number)) {
			return number;
		}

		if (property.ValueKind == JsonValueKind.String &&
			decimal.TryParse(property.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) {
			return parsed;
		}

		return 0m;
	}

	protected static List<string> SplitCsv(string line) {
		var result = new List<string>();
		var current = new System.Text.StringBuilder();
		var quoted = false;

		for (var i = 0; i < line.Length; i++) {
			var ch = line[i];

			if (ch == '"') {
				if (quoted && i + 1 < line.Length && line[i + 1] == '"') {
					current.Append('"');
					i++;
				}
				else {
					quoted = !quoted;
				}
			}
			else if (ch == ',' && !quoted) {
				result.Add(current.ToString().Trim());
				current.Clear();
			}
			else {
				current.Append(ch);
			}
		}

		result.Add(current.ToString().Trim());
		return result;
	}
}

