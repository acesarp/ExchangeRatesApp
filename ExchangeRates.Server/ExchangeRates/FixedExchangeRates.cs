using ExchangeRates.Domain.Enums;

namespace ExchangeRates.Server.ExchangeRates;

public static class FixedExchangeRates {
	private static readonly Dictionary<ECurrencyISO, decimal> UsdPegRates = new() {
		[ECurrencyISO.BMD] = 1.00m,
		[ECurrencyISO.BSD] = 1.00m,
		[ECurrencyISO.PAB] = 1.00m,
		[ECurrencyISO.BZD] = 2.00m,
		[ECurrencyISO.BBD] = 2.00m,
		[ECurrencyISO.XCD] = 2.70m
	};

	public static bool TryGetUsdRate(ECurrencyISO currency, out decimal rate) {
		return UsdPegRates.TryGetValue(currency, out rate);
	}
}
