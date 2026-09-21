using ExchangeRates.Infrastructure;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Models;

using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Server.Services;

public class HealthStatusService : IHealthStatusService {
	private readonly ILogger<HealthStatusService> _logger;
	private readonly ExchangeRatesDbContext _context;

	public HealthStatusService(ILogger<HealthStatusService> logger, ExchangeRatesDbContext context) {
		_logger = logger;
		_context = context;
	}
	public async Task<DbHealthResult> GetDbHealthAsync(CancellationToken ct) {
		try {
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			await _context.Database.ExecuteSqlRawAsync("SELECT 1", ct);
			stopwatch.Stop();

			return new DbHealthResult(true, stopwatch.Elapsed.TotalMilliseconds);

		}
		catch (Exception ex) {
			_logger.LogError(ex, "Database health check failed");
			return new DbHealthResult(false, 0, ex.Message);
		}
	}

}
