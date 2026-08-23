namespace ExchangeRates.Server.Interfaces;

public interface IExchangeRateService {
	Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(string provider, DateOnly? date = null, CancellationToken ct = default);
}