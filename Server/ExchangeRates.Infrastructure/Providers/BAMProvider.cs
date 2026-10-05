
using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System.Text.Json;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Bank Al-Maghrib
/// </summary>
public sealed class BAMProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.NativePerForeign;

	private readonly ILogger<BAMProvider> _logger;
	public BAMProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<BAMProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}

	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		var apiKey = ApiKey;

		if (string.IsNullOrWhiteSpace(apiKey)) {
			throw new InvalidOperationException("Missing ProviderKeys:BAM.");
		}

		var rates = new List<ExchangeRateResult>();

		// The API returns all currencies for a single fixing time; no fixings are published on weekends.
		for (var date = fromDate; date <= toDate; date = date.AddDays(1)) {
			if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) {
				continue;
			}

			var url = $"{ApiUrl.TrimEnd('/')}/cours/Version1/api/CoursVirement?date={date:yyyy-MM-dd}T12:30:00";
			using var response = await SendWithRateLimitRetryAsync(url, apiKey, ct);
			response.EnsureSuccessStatusCode();

			using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

			// [{ "date": "2026-10-01T12:30:00", "libDevise": "USD", "moyen": 9.7465, "uniteDevise": 1 }, ...]
			foreach (var row in doc.RootElement.EnumerateArray()) {
				var currencyCode = row.TryGetProperty("libDevise", out var c) ? c.GetString() : null;

				if (!string.Equals(currencyCode, quoteCurrency, StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				var unit = GetDecimal(row, "uniteDevise");
				var mid = GetDecimal(row, "moyen");

				if (mid <= 0) {
					var buy = GetDecimal(row, "achat");
					var sell = GetDecimal(row, "vente");

					if (buy > 0 && sell > 0) {
						mid = (buy + sell) / 2m;
					}
				}

				if (unit <= 0 || mid <= 0) {
					continue;
				}

				rates.Add(new ExchangeRateResult(date, Bank.NativeCurrency.CurrencyCode, quoteCurrency, mid / unit, Bank.BankCode));
			}
		}

		return rates;
	}

	/// <summary>
	/// The API subscription is rate limited per minute; when throttled (429) wait for the Retry-After interval and retry once.
	/// </summary>
	private async Task<HttpResponseMessage> SendWithRateLimitRetryAsync(string url, string apiKey, CancellationToken ct) {
		var response = await SendAsync();

		if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests) {
			return response;
		}

		var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(30);
		response.Dispose();
		_logger.LogDebug("BAM rate limit exceeded; retrying in {Delay}.", delay);
		await Task.Delay(delay > TimeSpan.FromSeconds(60) ? TimeSpan.FromSeconds(60) : delay, ct);

		return await SendAsync();

		Task<HttpResponseMessage> SendAsync() {
			var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", apiKey);
			return Http.SendAsync(request, ct);
		}
	}
}

