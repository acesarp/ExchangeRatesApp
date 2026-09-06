using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;

namespace ExchangeRates.Server.Services;

public sealed class ExchangeRateService : IExchangeRateService {
	private readonly ExchangeRateResolver _resolver;

	public ExchangeRateService(ExchangeRateResolver resolver) {
		_resolver = resolver;
	}

	public async Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		if (fromDate > toDate) {
			throw new ArgumentException("fromDate cannot be greater than toDate.");
		}

		if (baseCurrency == quoteCurrency) {
			return Enumerable.Range(0, toDate.DayNumber - fromDate.DayNumber + 1)
				.Select(i => new ExchangeRateResult(fromDate.AddDays(i), baseCurrency, quoteCurrency, 1m, "IDENTITY"))
				.ToList();
		}

		var directRates = await _resolver.GetDirectRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, ct);

		if (directRates.Count > 0) {
			return directRates;
		}

		return await _resolver.GetTriangulatedRatesAsync(baseCurrency, quoteCurrency, fromDate, toDate, ct);
	}
}