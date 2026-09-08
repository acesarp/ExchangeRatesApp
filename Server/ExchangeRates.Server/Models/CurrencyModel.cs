namespace ExchangeRates.Server.Models;

public sealed class CurrencyModel {
	public CurrencyModel(string code, int numericCode, string name, bool isHistoric, int priority) {
		Code = code;
		Name = name;
		IsHistoric = isHistoric;
	}

	public string Code { get; }
	public string Name { get; }
	public bool IsHistoric { get; }
}