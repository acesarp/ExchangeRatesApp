namespace ExchangeRates.Domain.Entities;

public sealed class PreferredProviderEntity {

	public int Id { get; set; }
	public int CurrencyId { get; set; }
	public int CentralBankId { get; set; }
	public int Priority { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedAtUtc { get; set; }

	public CurrencyEntity Currency { get; set; } = null!;
	public CentralBankEntity CentralBank { get; set; } = null!;
}