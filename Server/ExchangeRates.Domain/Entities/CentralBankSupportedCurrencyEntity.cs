namespace ExchangeRates.Domain.Entities;

public sealed class CentralBankSupportedCurrencyEntity {
	public int CentralBankId { get; set; }
	public int CurrencyId { get; set; }
	public string? ProviderSeriesId { get; set; }
	public CentralBankEntity CentralBank { get; set; } = null!;
	public CurrencyEntity Currency { get; set; } = null!;

}