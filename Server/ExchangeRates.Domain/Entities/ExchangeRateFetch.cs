using ExchangeRates.Domain.Enums;
namespace ExchangeRates.Domain.Entities;

public sealed class ExchangeRateFetch {
	public ExchangeRateFetch() {

	}
	public ExchangeRateFetch(string code, ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly fromDate, DateOnly toDate) {
		Provider = code;
		BaseCurrency = baseCurrency;
		QuoteCurrency = quoteCurrency;
		FromDate = fromDate;
		ToDate = toDate;
		FetchedAtUtc = DateTime.UtcNow;
	}
	public int Id { get; set; }
	public string Provider { get; set; } = null!;
	public ECurrencyISO BaseCurrency { get; set; }
	public ECurrencyISO QuoteCurrency { get; set; }
	public DateOnly FromDate { get; set; }
	public DateOnly ToDate { get; set; }
	public DateTime FetchedAtUtc { get; set; }
}