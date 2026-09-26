namespace ExchangeRates.Domain.Entities;

public class ProviderSeries {
	public string Currency { get; set; }
	public string SeriesId { get; set; } = string.Empty;
	public bool Invert { get; set; }
}