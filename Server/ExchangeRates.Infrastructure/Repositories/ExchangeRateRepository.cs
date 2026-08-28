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