using ExchangeRates.Domain.Entities;
using ExchangeRates.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExchangeRates.Infrastructure.Repositories;

public sealed class ExchangeRateRepository : IExchangeRateRepository {
	private readonly ExchangeRatesDbContext _context;
	private readonly ILogger<ExchangeRateRepository> _logger;

	public ExchangeRateRepository(ExchangeRatesDbContext context, ILogger<ExchangeRateRepository> logger) {
		_context = context;
		_logger = logger;
	}

	/// <summary> Gets the exchange rates from database for the specified base and quote currencies within the given date range </summary>
	public async Task<IReadOnlyList<ExchangeRateEntity>> GetRatesAsync(string baseCurrencyCode, string quoteCurrencyCode, DateOnly fromDate, DateOnly toDate, CancellationToken ct) {
		baseCurrencyCode = baseCurrencyCode.ToUpperInvariant();
		quoteCurrencyCode = quoteCurrencyCode.ToUpperInvariant();
		return await _context.ExchangeRates.AsNoTracking()
																	.Where(x => (x.BaseCurrency.CurrencyCode == baseCurrencyCode && x.QuoteCurrency.CurrencyCode == quoteCurrencyCode ||
																													x.BaseCurrency.CurrencyCode == quoteCurrencyCode && x.QuoteCurrency.CurrencyCode == baseCurrencyCode) &&
																										x.Date >= fromDate && x.Date <= toDate)

																	.Include(x => x.BaseCurrency)
																	.Include(x => x.QuoteCurrency)
																	.OrderBy(x => x.Date)
																	.ToListAsync(ct);
	}

	/// <summary> Add a range of exchange rates to the database. </summary>
	public async Task<int> AddRangeAsync(IEnumerable<ExchangeRateEntity> rates, CancellationToken ct) {
		var items = rates.DistinctBy(x => new { x.Date, x.BaseCurrencyId, x.QuoteCurrencyId }).ToList();
		if (items.Count == 0) {
			return 0;
		}

		var minDate = items.Min(x => x.Date);
		var maxDate = items.Max(x => x.Date);
		var baseCurrencies = items.Select(x => x.BaseCurrencyId).Distinct().ToList();
		var quoteCurrencies = items.Select(x => x.QuoteCurrencyId).Distinct().ToList();

		var existing = await _context.ExchangeRates.AsNoTracking()
			.Where(x => x.Date >= minDate && x.Date <= maxDate && baseCurrencies.Contains(x.BaseCurrencyId) && quoteCurrencies.Contains(x.QuoteCurrencyId))
			.Select(x => new { x.Date, x.BaseCurrencyId, x.QuoteCurrencyId })
			.ToListAsync(ct);

		var existingKeys = existing.Select(x => (x.Date, x.BaseCurrencyId, x.QuoteCurrencyId)).ToHashSet();
		var newRates = items.Where(x => !existingKeys.Contains((x.Date, x.BaseCurrencyId, x.QuoteCurrencyId))).ToList();

		if (newRates.Count == 0) {
			return 0;
		}

		// Set navigation properties to null to avoid EF Core tracking issues
		foreach (var rate in newRates) {
			rate.BaseCurrency = null!;
			rate.QuoteCurrency = null!;
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

	public async Task<IReadOnlyList<CurrencyEntity>> GetCurrencyByCodesAsync(IEnumerable<string> codes, CancellationToken ct) {
		return await _context.Currencies.AsNoTracking().Where(x => codes.Contains(x.CurrencyCode)).ToListAsync(ct);
	}

	/// <summary>
	/// Retrieve all available central banks from database.
	/// </summary>
	public async Task<IEnumerable<CentralBankEntity>> GetCentralBanksAsync(CancellationToken ct) {
		return await _context.CentralBanks.AsNoTracking().ToListAsync(ct);
	}

	public async Task<CentralBankEntity> FindSuitableBankAsync(string currency1, string currency2, CancellationToken ct) {
		currency1 = currency1.ToUpperInvariant();
		currency2 = currency2.ToUpperInvariant();
		var query = _context.CentralBanks.AsNoTracking()
																.Include(x => x.NativeCurrency)
																.Where(x => x.IsActive &&
																									((x.NativeCurrency.CurrencyCode == currency1 &&
																										 x.SupportedCurrencies.Any(sc => sc.Currency.CurrencyCode == currency2))
																										||
																										(x.NativeCurrency.CurrencyCode == currency2 &&
																										 x.SupportedCurrencies.Any(sc => sc.Currency.CurrencyCode == currency1))
																									)
																)
																.OrderBy(x => x.Priority ?? int.MaxValue)
																.ThenBy(x => x.BankCode);
		var result = await query.FirstOrDefaultAsync(ct);

		if (result != null) {
			return result;
		}
		throw new InvalidOperationException($"No provider available for {currency1}/{currency2}.");
	}

	/// <summary> Add a range of unavailable dates to the database.<br /> Weekends and holidays when the exchange rate is not available. </summary>
	public async Task<int> AddUnavailableDatesAsync(IEnumerable<ExchangeRateUnavailableDateEntity> unavailableDates, CancellationToken ct) {
		var uniqueDates = unavailableDates.DistinctBy(x => new { x.CentralBankId, x.BaseCurrency, x.QuoteCurrency, x.UnavailableDate }).ToList();
		_context.ExchangeRateUnavailableDates.AddRange(uniqueDates);
		return await _context.SaveChangesAsync(ct);
	}

	/// <summary> Get a range of unavailable date entities from the database.<br /> Weekends and holidays when the exchange rate is not available </summary>
	public async Task<HashSet<ExchangeRateUnavailableDateEntity>> GetUnavailableDateEntitiesAsync(DateOnly from, DateOnly to, CancellationToken ct) {
		return await _context.ExchangeRateUnavailableDates.Where(x => x.UnavailableDate >= from &&
																																							x.UnavailableDate <= to)
																							.ToHashSetAsync(ct);
	}

	public async Task<HashSet<DateOnly>> GetUnavailableDatesAsync(DateOnly from, DateOnly to, CancellationToken ct) {
		return await _context.ExchangeRateUnavailableDates.Where(x => x.UnavailableDate >= from &&
																																					x.UnavailableDate <= to)
																					.Select(x => x.UnavailableDate)
																					.ToHashSetAsync(ct);
	}

	public async Task<CentralBankEntity> GetCentralBankAsync(string bankCode, CancellationToken ct) {
		return await _context.CentralBanks.AsNoTracking()
																.FirstOrDefaultAsync(x => x.BankCode == bankCode, ct);
	}
}