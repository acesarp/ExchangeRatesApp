namespace ExchangeRates.Domain.Entities;

public sealed class CentralBankEntity {
	public int Id { get; set; }
	public string Code { get; set; } = null!;
	public string Name { get; set; } = null!;
	public string CountryOfOrigin { get; set; }
	public int CurrencyId { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedAtUtc { get; set; }
	public int? Priority { get; set; }

	public CurrencyEntity Currency { get; set; } = null!;
}
