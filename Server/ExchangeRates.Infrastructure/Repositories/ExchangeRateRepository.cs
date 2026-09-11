using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Infrastructure.Repositories;

public sealed class ExchangeRateRepository : IExchangeRateRepository {
	private readonly ExchangeRatesDbContext _context;

	public ExchangeRateRepository(ExchangeRatesDbContext context) {
		_context = context;
	}

	/// <summary>
	/// Retrieve rates for the given base and quote currency from database.
	/// </summary>>
	public async Task<IReadOnlyList<ExchangeRateEntity>> GetRatesAsync(string baseCurrency, string quoteCurrency, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
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

	/// <summary> Add a range of exchange rates to the database. </summary>
	public async Task<int> AddRangeAsync(IEnumerable<ExchangeRateEntity> rates, CancellationToken ct) {
		var items = rates.DistinctBy(x => new { x.Date, x.BaseCurrency, x.QuoteCurrency }).ToList();
		if (items.Count == 0) {
			return 0;
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
			return 0;
		}

		_context.ExchangeRates.AddRange(newRates);
		return await _context.SaveChangesAsync(ct);
	}

	/// <summary>
	/// Retrieve all available currencies from database.
	/// </summary>
	public async Task<IReadOnlyList<CurrencyEntity>> GetCurrenciesAsync(CancellationToken ct) {
		return await _context.Currencies.AsNoTracking().ToListAsync(ct);
	}

	/// <summary>
	/// Retrieve all available central banks from database.
	/// </summary>
	public async Task<IEnumerable<CentralBankEntity>> GetCentralBanksAsync(CancellationToken ct) {
		return await _context.CentralBanks.AsNoTracking().ToListAsync(ct);
	}
}