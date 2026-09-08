using ExchangeRates.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Infrastructure.Repositories;

public sealed class CentralBankRepository {
	private readonly ExchangeRatesDbContext _context;

	public CentralBankRepository(ExchangeRatesDbContext context) {
		_context = context;
	}

	public async Task<IReadOnlyList<CentralBankEntity>> GetCentralBanksAsync(CancellationToken ct) {
		// This is a placeholder implementation. Replace with actual data retrieval logic.
		return await _context.CentralBanks.AsNoTracking().ToListAsync(ct);
	}
}
