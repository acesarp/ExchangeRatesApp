using ExchangeRates.Domain.Models;

namespace ExchangeRates.Domain.Interfaces;

public interface ICryptoMarketDataProvider {
	string Name { get; }

	Task ConnectAsync(IEnumerable<string> symbols, Func<CryptoPriceResult, CancellationToken, Task> onPriceReceived, CancellationToken ct);
}