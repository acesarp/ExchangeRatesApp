namespace ExchangeRates.Server;

public sealed record ExchangeRate(DateOnly Date, string BaseCurrency, string QuoteCurrency, decimal Rate, string Provider);

public interface ICentralBankProvider {
	string Code { get; }
	string Name { get; }
	string NativeCurrency { get; }

	Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(DateOnly? date = null, CancellationToken ct = default);
}

