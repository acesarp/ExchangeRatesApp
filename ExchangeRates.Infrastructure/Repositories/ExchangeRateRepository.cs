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

	public async Task<IReadOnlyList<ExchangeRate>> GetAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {

		return await _context.ExchangeRates.AsNoTracking()
																	.Where(x =>
																		x.BaseCurrency == baseCurrency &&
																		x.QuoteCurrency == quoteCurrency &&
																		x.Date >= fromDate &&
																		x.Date <= toDate)
																	.OrderBy(x => x.Date)
																	.ToListAsync(ct);
	}

	public async Task AddRangeAsync(IEnumerable<ExchangeRate> rates, CancellationToken ct) {

		_context.ExchangeRates.AddRange(rates);
		await _context.SaveChangesAsync(ct);
	}
}