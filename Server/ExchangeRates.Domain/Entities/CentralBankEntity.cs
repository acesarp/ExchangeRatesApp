namespace ExchangeRates.Domain.Entities;

public sealed class CentralBankEntity {

	public CentralBankEntity(string bankCode, string bankName, string countryOfOrigin, int currencyId, bool isActive, DateTime createdAtUtc) {
		BankCode = bankCode;
		BankName = bankName;
		CountryOfOrigin = countryOfOrigin;
		CurrencyId = currencyId;
		IsActive = isActive;
		CreatedAtUtc = createdAtUtc;
	}

	/// <summary>
	/// Internal database identifier for the central bank.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Unique code used to identify the central bank or exchange-rate provider (e.g. BOK, ECB, BOE).
	/// </summary>
	public string BankCode { get; set; } = null!;

	/// <summary>
	/// Official name of the central bank or exchange-rate provider.
	/// </summary>
	public string BankName { get; set; } = null!;

	/// <summary>
	/// Country or territory where the central bank or provider originates.
	/// </summary>
	public string CountryOfOrigin { get; set; }

	/// <summary>
	/// Foreign key identifying the bank's native currency.
	/// </summary>
	public int CurrencyId { get; set; }

	/// <summary>
	/// Indicates whether the central bank or provider is currently active and available for exchange-rate retrieval.
	/// </summary>
	public bool IsActive { get; set; }

	/// <summary>
	/// UTC date and time when the central bank record was created.
	/// </summary>
	public DateTime CreatedAtUtc { get; set; }

	/// <summary>
	/// Defines the preferred order in which providers are selected.
	/// A lower value represents a higher provider priority.
	/// </summary>
	public int? Priority { get; set; }

	/// <summary>
	/// Navigation property representing the bank's native currency.
	/// </summary>
	public CurrencyEntity Currency { get; set; } = null!;
}