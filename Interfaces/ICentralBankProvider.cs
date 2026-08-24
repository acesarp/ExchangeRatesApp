using ExchangeRates.Server.Enums;

namespace ExchangeRates.Server.Interfaces;

public interface ICentralBankProvider {
	string Code { get; }
	string Name { get; }
	string NativeCurrency { get; }

	/// <summary>
	/// Retrieves exchange rates for the specified date.
	/// Uses the current UTC date when no date is provided.
	/// </summary>
	Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(DateOnly? date, CancellationToken ct);
	IReadOnlySet<ECurrency> SupportedCurrencies { get; }
}
