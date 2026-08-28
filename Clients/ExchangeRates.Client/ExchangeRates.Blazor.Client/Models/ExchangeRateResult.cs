namespace ExchangeRates.Blazor.Client.Models;

public class ExchangeRateResult {
	public DateOnly Date { get; set; }
	public string BaseCurrency { get; set; } = string.Empty;
	public string QuoteCurrency { get; set; } = string.Empty;
	public decimal Rate { get; set; }
	public string Provider { get; set; } = string.Empty;
}
