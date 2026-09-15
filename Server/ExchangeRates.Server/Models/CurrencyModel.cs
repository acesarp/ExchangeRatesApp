namespace ExchangeRates.Server.Models;

public sealed class CurrencyModel {
	public CurrencyModel(string currencyCode, int numericCode, string name, bool isHistoric) {
		CurrencyCode = currencyCode;
		Name = name;
		IsHistoric = isHistoric;
		NumericCode = numericCode;
	}

	public string CurrencyCode { get; }
	public string Name { get; }
	public bool IsHistoric { get; }
	public int NumericCode { get; }
}