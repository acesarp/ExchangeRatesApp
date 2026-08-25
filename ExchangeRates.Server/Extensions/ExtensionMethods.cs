using ExchangeRates.Server.Enums;

namespace ExchangeRates.Server.Extensions;

public static class ExtensionMethods {

	private static readonly Dictionary<string, string> CurrencyAliases = new(StringComparer.OrdinalIgnoreCase) {
		["CNH"] = "CNY"
	};

	public static T ToEnum<T>(this string text) where T : struct, Enum {
		return Enum.Parse<T>(text, ignoreCase: true);
	}

	public static ECurrencyISO ToECurrency(this string text) {
		var code = text.Trim();

		if (CurrencyAliases.TryGetValue(code, out var normalized)) {
			code = normalized;
		}

		return Enum.Parse<ECurrencyISO>(code, ignoreCase: true);
	}
}
