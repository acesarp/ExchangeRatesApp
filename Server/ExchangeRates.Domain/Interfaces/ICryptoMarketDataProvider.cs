using ExchangeRates.Domain.Models;

namespace ExchangeRates.Domain.Interfaces;

public interface ICryptoMarketDataProvider {
	string Name { get; }

	Task ConnectAsync(IEnumerable<string> symbols, Func<CryptoPriceResult, Task> onPriceReceived, CancellationToken ct);
}