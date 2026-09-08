using ExchangeRates.Domain.Entities;

namespace ExchangeRates.Server.Models;

public sealed class CentralBankModel {
	public CentralBankModel(string code, string bankName, string countryOfOrigin, string nativeCurrency, int currencyId, int? priority) {
		Code = code;
		BankName = bankName;
		CountryOfOrigin = countryOfOrigin;
		NativeCurrency = nativeCurrency;
		CurrencyId = currencyId;
		Priority = priority;
	}

	public string Code { get; }
	public string BankName { get; }
	public string CountryOfOrigin { get; }
	public string NativeCurrency { get; }
	public int CurrencyId { get; set; }
	public bool IsActive { get; set; }
	public int? Priority { get; set; }

	public CurrencyEntity Currency { get; set; } = null!;
}