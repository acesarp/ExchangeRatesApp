using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExchangeRates.Domain.Entities;

public sealed class CentralBankEntity {
	public CentralBankEntity() { }

	public CentralBankEntity(string bankCode, string bankName, string countryOfOrigin, int nativeCurrencyId, string? apiUrl, bool isActive, DateTime createdAtUtc, IReadOnlySet<ProviderSeries>? series = null) {
		BankCode = bankCode;
		BankName = bankName;
		CountryOfOrigin = countryOfOrigin;
		CurrencyId = nativeCurrencyId;
		ApiUrl = apiUrl;
		IsActive = isActive;
		CreatedAtUtc = createdAtUtc;
		Series = series;
	}

	/// <summary> Internal database identifier for the central bank </summary>
	[Key]
	public int Id { get; set; }

	/// <summary>
	/// Unique code used to identify the central bank or exchange-rate provider (e.g. BOK, ECB, BOE).
	/// </summary>
	public string BankCode { get; set; }

	/// <summary>
	/// Official name of the central bank or exchange-rate provider.
	/// </summary>
	public string BankName { get; set; } = null!;

	/// <summary>
	/// Country or territory where the central bank or provider originates.
	/// </summary>
	public string CountryOfOrigin { get; set; } = null!;

	/// <summary> Base URL for the central bank's API </summary>
	public string? ApiUrl { get; set; }

	/// <summary> Foreign key identifying the bank's native currency. </summary>
	[DisplayName("Native Currency Id")]
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
	/// Defines the preferred order in which providers are selected.<br />
	/// A lower value represents a higher provider priority.
	/// </summary>
	public int? Priority { get; set; }

	[NotMapped]
	public IReadOnlySet<ProviderSeries>? Series { get; set; }
	/// <summary>
	/// Navigation property representing the bank's native currency.
	/// </summary>
	public CurrencyEntity NativeCurrency { get; set; } = null!;

	[NotMapped]
	public string NativeCurrencyCode => NativeCurrency.CurrencyCode;

	/// <summary> Currencies for which this provider can retrieve exchange rates </summary>
	public ICollection<CentralBankSupportedCurrencyEntity> SupportedCurrencies { get; set; } = new List<CentralBankSupportedCurrencyEntity>();
}