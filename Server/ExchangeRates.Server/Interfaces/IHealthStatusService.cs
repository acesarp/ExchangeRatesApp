using ExchangeRates.Server.Models;

namespace ExchangeRates.Server.Interfaces;

public interface IHealthStatusService {
	Task<DbHealthResult> GetDbHealthAsync(CancellationToken ct);
}
