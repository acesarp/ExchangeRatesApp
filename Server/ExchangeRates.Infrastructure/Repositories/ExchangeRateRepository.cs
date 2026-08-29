using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Enums;
using ExchangeRates.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Infrastructure.Repositories;

public sealed class ExchangeRateRepository : IExchangeRateRepository {
	private readonly ExchangeRatesDbContext _context;

	public ExchangeRateRepository(ExchangeRatesDbContext context) {
		_context = context;
	}

	public async Task<IReadOnlyList<ExchangeRateEntity>> GetAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		return await _context.ExchangeRates.AsNoTracking()
																	.Where(x =>
																		x.BaseCurrency == baseCurrency &&
																		x.QuoteCurrency == quoteCurrency &&
																		x.Date >= fromDate &&
																		x.Date <= toDate)
																	.OrderBy(x => x.Date)
																	.ToListAsync(ct);
	}

	public async Task AddRangeAsync(IEnumerable<ExchangeRateEntity> rates, CancellationToken ct) {

		_context.ExchangeRates.AddRange(rates);
		await _context.SaveChangesAsync(ct);
	}

	/// <summary>
	/// Retrieves the recorded exchange-rate fetch operations for a specific provider and currency pair
	/// that are fully contained within the requested date range.
	/// </summary>
	/// <param name="provider">The code or name of the provider that performed the exchange-rate fetch.</param>
	/// <param name="baseCurrency">The base currency of the requested exchange-rate pair.</param>
	/// <param name="quoteCurrency">The quote currency of the requested exchange-rate pair.</param>
	/// <param name="fromDate">The beginning of the date range used to filter fetch records.</param>
	/// <param name="toDate">The end of the date range used to filter fetch records.</param>
	/// <param name="ct">A cancellation token that can be used to cancel the asynchronous database operation.</param>
	/// <returns>
	/// A read-only list of <see cref="ExchangeRateFetch"/> records matching the provider and currency pair,
	/// where each fetch starts on or after <paramref name="fromDate"/> and ends on or before
	/// <paramref name="toDate"/>. Results are ordered by <see cref="ExchangeRateFetch.FromDate"/>.
	/// </returns>
	/// <remarks>
	/// A fetch is included only when its entire recorded interval falls within the requested interval:
	/// <c>FromDate &gt;= fromDate</c> and <c>ToDate &lt;= toDate</c>.
	/// Fetches that only partially overlap the requested interval are not returned.
	/// </remarks>
	public async Task<IReadOnlyList<ExchangeRateFetch>> GetFetchesAsync(string provider, ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		return await _context.ExchangeRateFetches.AsNoTracking()
																	.Where(x =>
																		x.Provider == provider &&
																		x.BaseCurrency == baseCurrency &&
																		x.QuoteCurrency == quoteCurrency &&
																		x.FromDate >= fromDate &&
																		x.ToDate <= toDate)
																	.OrderBy(x => x.FromDate)
																	.ToListAsync(ct);
	}

	public async Task AddFetchAsync(ExchangeRateFetch fetch, CancellationToken ct) {
		_context.ExchangeRateFetches.Add(fetch);
		await _context.SaveChangesAsync(ct);
	}
}