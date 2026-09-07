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

	public async Task<IReadOnlyList<ExchangeRateEntity>> GetRatesAsync(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		return await _context.ExchangeRates.AsNoTracking()
																	.Where(x => (x.BaseCurrency == baseCurrency && x.QuoteCurrency == quoteCurrency ||
																													x.BaseCurrency == quoteCurrency && x.QuoteCurrency == baseCurrency) &&
																										x.Date >= fromDate && x.Date <= toDate)
																	.Select(s => new ExchangeRateEntity {
																		Date = s.Date,
																		BaseCurrency = s.BaseCurrency,
																		QuoteCurrency = s.QuoteCurrency,
																		Rate = s.Rate
																	})
																	.OrderBy(x => x.Date)
																	.ToListAsync(ct);
	}

	public async Task AddRangeAsync(IEnumerable<ExchangeRateEntity> rates, CancellationToken ct) {
		var items = rates.DistinctBy(x => new { x.Date, x.BaseCurrency, x.QuoteCurrency }).ToList();
		if (items.Count == 0) {
			return;
		}

		var minDate = items.Min(x => x.Date);
		var maxDate = items.Max(x => x.Date);
		var baseCurrencies = items.Select(x => x.BaseCurrency).Distinct().ToList();
		var quoteCurrencies = items.Select(x => x.QuoteCurrency).Distinct().ToList();

		var existing = await _context.ExchangeRates.AsNoTracking()
			.Where(x => x.Date >= minDate && x.Date <= maxDate && baseCurrencies.Contains(x.BaseCurrency) && quoteCurrencies.Contains(x.QuoteCurrency))
			.Select(x => new { x.Date, x.BaseCurrency, x.QuoteCurrency })
			.ToListAsync(ct);

		var existingKeys = existing.Select(x => (x.Date, x.BaseCurrency, x.QuoteCurrency)).ToHashSet();
		var newRates = items.Where(x => !existingKeys.Contains((x.Date, x.BaseCurrency, x.QuoteCurrency))).ToList();

		if (newRates.Count == 0) {
			return;
		}

		_context.ExchangeRates.AddRange(newRates);
		await _context.SaveChangesAsync(ct);
	}
}