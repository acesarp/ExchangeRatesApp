namespace ExchangeRates.Server.Interfaces;

public interface IKrakenMarketDataService {
	Task ConnectAsync(CancellationToken ct);
}