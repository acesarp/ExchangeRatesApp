namespace ExchangeRates.Server.Interfaces;

public interface ICentralBankProvider {
	string Code { get; }
	string Name { get; }
	string NativeCurrency { get; }
	Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(DateOnly? date, CancellationToken ct);
}
