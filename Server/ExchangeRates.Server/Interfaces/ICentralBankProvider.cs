namespace ExchangeRates.Server.Interfaces;

public interface ICentralBankProvider {

	List<string> HistoricCurrencies { get; }
	string PivotCurrency { get; }
	string BankCode { get; }
	string NativeCurrencyCode { get; }
	string BankName { get; }
	string? CountryOfOrigin { get; }
	int? Priority { get; }

	/// <summary>
	/// Retrieves exchange rates for the specified date.
	/// Uses the current UTC date when no date is provided.
	/// </summary>
	Task<IReadOnlyList<ExchangeRateResult>> GetRatesAsync(string currency, DateOnly fromDate, DateOnly toDate, CancellationToken ct);
	IReadOnlySet<string> SupportedCurrencies { get; }
	bool Supports(string currency);
}
