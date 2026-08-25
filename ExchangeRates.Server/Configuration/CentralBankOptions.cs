namespace ExchangeRates.Server.Configuration;


public class CentralBankOptions {
	public required string Name { get; set; }
	public required string Url { get; set; }
	public Dictionary<string, CentralBankOptions> Providers { get; set; } = [];
}
