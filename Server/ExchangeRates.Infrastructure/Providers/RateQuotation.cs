namespace ExchangeRates.Infrastructure.Providers;

/// <summary>
/// The direction in which a central bank source publishes its exchange rates.
/// </summary>
public enum RateQuotation {
	/// <summary> Native currency units per one unit of the foreign currency, e.g. CNB: 1 USD = 21.83 CZK. Most central banks quote this way. </summary>
	NativePerForeign,

	/// <summary> Foreign currency units per one unit of the native currency, e.g. ECB: 1 EUR = 1.12 USD. </summary>
	ForeignPerNative
}
