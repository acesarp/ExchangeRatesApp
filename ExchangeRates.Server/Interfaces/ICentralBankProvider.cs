using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.Interfaces;

public interface ICentralBankProvider {
	string Code { get; }
	string Name { get; }
	ECurrencyISO NativeCurrency { get; }

	/// <summary>
	/// Retrieves exchange rates for the specified date.
	/// Uses the current UTC date when no date is provided.
	/// </summary>
	Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(ECurrencyISO currency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);
	IReadOnlySet<ECurrencyISO> SupportedCurrencies { get; }
	bool Supports(ECurrencyISO currency);
}
