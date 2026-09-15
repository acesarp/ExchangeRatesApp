namespace ExchangeRates.Domain.Entities;

public sealed class CurrencyEntity {
	public CurrencyEntity() {

	}
	public CurrencyEntity(int id) {
		Id = id;
	}
	public CurrencyEntity(string currencyCode) {
		CurrencyCode = currencyCode;
	}
	public CurrencyEntity(string currencyCode, short numericCode, string name, bool isHistoric, int priority) {
		CurrencyCode = currencyCode;
		NumericCode = numericCode;
		Name = name;
		IsHistoric = isHistoric;
		Priority = priority;
	}

	/// <summary>
	/// Internal database identifier for the currency.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// ISO 4217 three-letter alphabetic currency code (e.g. USD, EUR, GBP).
	/// </summary>
	public string CurrencyCode { get; set; }

	/// <summary>
	/// ISO 4217 three-digit numeric currency code (e.g. 840 for USD, 978 for EUR).
	/// </summary>
	public short NumericCode { get; set; }

	/// <summary>
	/// Official or commonly recognized name of the currency.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Indicates whether the currency is historical and no longer actively used.
	/// </summary>
	public bool IsHistoric { get; set; }

	/// <summary>
	/// Defines the canonical ordering of currencies when storing exchange-rate pairs.
	/// A lower value has higher priority and is used as the base currency.
	/// </summary>
	public int Priority { get; set; }

	public ICollection<CentralBankSupportedCurrencyEntity> SupportedByCentralBanks { get; set; } = new List<CentralBankSupportedCurrencyEntity>();
}