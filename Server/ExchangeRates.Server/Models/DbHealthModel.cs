namespace ExchangeRates.Server.Models;

public sealed record DbHealthResult(bool CanConnect, double LatencyMs, string? Error = null);