namespace ExchangeRates.Domain.Entities;

public sealed class FixedExchangeRateEntity {
	public int Id { get; set; }
	public int CurrencyId { get; set; }
	public int PeggedOnCurrencyId { get; set; }
	public decimal Rate { get; set; }
	public bool IsActive { get; set; }

	public CurrencyEntity Currency { get; set; } = null!;
	public CurrencyEntity PeggedOnCurrency { get; set; } = null!;
	public DateOnly? ValidFrom { get; set; }
	public DateOnly? ValidTo { get; set; }
}