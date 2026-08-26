namespace ExchangeRates.Infrastructure;

public sealed class ExchangeRateEntity {
	public int Id { get; set; }
	public DateOnly Date { get; set; }
	public ECurrencyISO BaseCurrency { get; set; }
	public ECurrencyISO QuoteCurrency { get; set; }
	public decimal Rate { get; set; }
	public string Provider { get; set; } = null!;
}