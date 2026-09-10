using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Models;

public sealed class CentralBankModel {
	public CentralBankModel(string bankCode, string bankName, string countryOfOrigin, string nativeCurrency, int currencyId, int? priority) {
		BankCode = bankCode;
		BankName = bankName;
		CountryOfOrigin = countryOfOrigin;
		NativeCurrency = nativeCurrency;
		CurrencyId = currencyId;
		Priority = priority;
	}

	public string BankCode { get; }
	public string BankName { get; }
	public string CountryOfOrigin { get; }
	public string NativeCurrency { get; }
	public int CurrencyId { get; set; }
	public bool IsActive { get; set; }
	public int? Priority { get; set; }

	public CurrencyEntity Currency { get; set; } = null!;
}