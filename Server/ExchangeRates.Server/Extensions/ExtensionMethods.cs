namespace ExchangeRates.Server.Extensions;

public static class ExtensionMethods {

	private static readonly Dictionary<string, string> CurrencyAliases = new(StringComparer.OrdinalIgnoreCase) {
		["CNH"] = "CNY"
	};

}
