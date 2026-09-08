namespace ExchangeRates.Domain.Entities;

public sealed class CurrencyEntity {
	public CurrencyEntity(int id, string code, short numericCode, string name, bool isHistoric, int priority) {
		Id = id;
		Code = code;
		NumericCode = numericCode;
		Name = name;
		IsHistoric = isHistoric;
		Priority = priority;
	}
	public int Id { get; set; }
	public string Code { get; set; }
	public short NumericCode { get; set; }
	public string Name { get; set; }
	public bool IsHistoric { get; set; }
	public int Priority { get; set; }
}