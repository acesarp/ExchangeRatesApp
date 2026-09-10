namespace ExchangeRates.Domain.Entities;

public sealed class CentralBankEntity {

	public CentralBankEntity() {

	}

	public CentralBankEntity(string BankCode, string BankName, string CountryOfOrigin, int CurrencyId, bool IsActive, DateTime CreatedAtUtc) {
		this.BankCode = BankCode;
		this.BankName = BankName;
		this.CountryOfOrigin = CountryOfOrigin;
		this.CurrencyId = CurrencyId;
		this.IsActive = IsActive;
		this.CreatedAtUtc = CreatedAtUtc;
	}

	public int Id { get; set; }
	public string BankCode { get; set; } = null!;

	public string BankName { get; set; } = null!;
	public string CountryOfOrigin { get; set; }
	public int CurrencyId { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedAtUtc { get; set; }
	public int? Priority { get; set; }

	public CurrencyEntity Currency { get; set; } = null!;
}
