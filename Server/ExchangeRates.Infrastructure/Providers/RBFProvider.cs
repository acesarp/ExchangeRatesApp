
using ExchangeRates.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ExchangeRates.Domain.Models;
namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// Reserve Bank of Fiji
/// </summary>
public sealed class RBFProvider : CentralBankProviderBase {
	protected override RateQuotation Quotation => RateQuotation.ForeignPerNative;

	private readonly ILogger<RBFProvider> _logger;

	public RBFProvider(HttpClient http, IConfiguration configuration, CentralBankEntity bank, ILogger<RBFProvider> logger) : base(http, bank, configuration) {
		_logger = logger;
	}
	protected override async Task<IReadOnlyList<ExchangeRateResult>> FetchAsync(string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		_logger.LogWarning("RBF endpoint requires manual data discovery; parsing not supported.");
		return await Task.FromResult<IReadOnlyList<ExchangeRateResult>>(Array.Empty<ExchangeRateResult>());
	}
}

