
using ExchangeRates.Domain.Enums;

using System.ComponentModel.DataAnnotations.Schema;

namespace ExchangeRates.Domain.Entities;

public sealed class ExchangeRateEntity {
	public int Id { get; set; }
	public DateOnly Date { get; set; }
	public ECurrencyISO BaseCurrency { get; set; }
	public ECurrencyISO QuoteCurrency { get; set; }
	public decimal Rate { get; set; }
	[NotMapped]
	public string Provider { get; set; } = null!;
}