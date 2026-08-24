using ExchangeRates.Server.Enums;

namespace ExchangeRates.Server.Interfaces;

public interface ICentralBankProvider {
	string Code { get; }
	string Name { get; }
	ECurrency NativeCurrency { get; }

	/// <summary>
	/// Retrieves exchange rates for the specified date.
	/// Uses the current UTC date when no date is provided.
	/// </summary>
	Task<IReadOnlyList<ExchangeRate>> GetRatesAsync(ECurrency currency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);
	IReadOnlySet<ECurrency> SupportedCurrencies { get; }
	bool Supports(ECurrency currency);
}
