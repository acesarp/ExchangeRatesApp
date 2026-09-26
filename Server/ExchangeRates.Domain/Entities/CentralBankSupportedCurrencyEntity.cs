using System.ComponentModel.DataAnnotations.Schema;

namespace ExchangeRates.Domain.Entities;


public sealed class CentralBankSupportedCurrencyEntity {
	[ForeignKey(nameof(CentralBank))]
	public int CentralBankId { get; set; }
	[ForeignKey(nameof(Currency))]
	public int CurrencyId { get; set; }
	public string? ProviderSeriesId { get; set; }
	public CentralBankEntity CentralBank { get; set; } = null!;
	public CurrencyEntity Currency { get; set; } = null!;

}