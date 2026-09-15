using System.ComponentModel.DataAnnotations.Schema;

namespace ExchangeRates.Domain.Entities;

public sealed class ExchangeRateEntity {

	/// <summary>
	/// Internal database identifier for the exchange rate.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	/// Date for which the exchange rate is valid.
	/// </summary>
	public required DateOnly Date { get; set; }

	public int BaseCurrencyId { get; set; }
	/// <summary>
	/// ISO 4217 currency code of the base currency.
	/// Represents the currency whose value is equal to one unit.
	/// </summary>
	public required CurrencyEntity BaseCurrency { get; set; }

	public int QuoteCurrencyId { get; set; }
	/// <summary>
	/// ISO 4217 currency code of the quote currency.
	/// Represents the currency in which the base currency is expressed.
	/// </summary>
	public required CurrencyEntity QuoteCurrency { get; set; }

	/// <summary>
	/// Exchange rate expressed as the amount of quote currency equivalent to one unit of the base currency.
	/// For example, USD/CAD = 1.37 means 1 USD = 1.37 CAD.
	/// </summary>
	public decimal Rate { get; set; }

	/// <summary>
	/// Code identifying the provider from which the exchange rate was obtained.
	/// This property is not persisted in the database.
	/// </summary>
	[NotMapped]
	public string Provider { get; set; } = null!;
}