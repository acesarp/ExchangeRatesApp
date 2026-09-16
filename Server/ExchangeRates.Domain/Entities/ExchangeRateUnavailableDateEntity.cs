using System.ComponentModel.DataAnnotations;

namespace ExchangeRates.Domain.Entities;

public sealed class ExchangeRateUnavailableDateEntity {
	[Key]
	public long Id { get; set; }
	public int CentralBankId { get; set; }
	public string BaseCurrency { get; set; } = string.Empty;
	public string QuoteCurrency { get; set; } = string.Empty;
	public DateOnly UnavailableDate { get; set; }
	public DateTime CreatedAtUTC { get; set; }
}
