using ExchangeRates.Domain.Enums;
namespace ExchangeRates.Domain.Entities;

public sealed class ExchangeRateFetch {
	public int Id { get; set; }
	public string Provider { get; set; } = null!;
	public ECurrencyISO BaseCurrency { get; set; }
	public ECurrencyISO QuoteCurrency { get; set; }
	public DateOnly FromDate { get; set; }
	public DateOnly ToDate { get; set; }
	public DateTime FetchedAtUtc { get; set; }
}