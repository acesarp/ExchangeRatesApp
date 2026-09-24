using ExchangeRates.Domain.Models;

namespace ExchangeRates.Domain.Interfaces;

public sealed class CoinbaseMarketDataProvider : ICryptoMarketDataProvider {
	public string Name => throw new NotImplementedException();

	public Task ConnectAsync(IEnumerable<string> symbols, Func<CryptoPriceResult, Task> onPriceReceived, CancellationToken ct) {
		throw new NotImplementedException();
	}
}
