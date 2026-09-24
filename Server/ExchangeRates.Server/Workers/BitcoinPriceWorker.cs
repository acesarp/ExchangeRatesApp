using ExchangeRates.Domain.Interfaces;

namespace ExchangeRates.Server.Workers;

public class BitcoinPriceWorker : BackgroundService {
	private readonly ICryptoMarketDataProvider _cryptoMarketDataProvider;
	private readonly ILogger<BitcoinPriceWorker> _logger;
	private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

	public BitcoinPriceWorker(ICryptoMarketDataProvider cryptoMarketDataProvider, ILogger<BitcoinPriceWorker> logger) {
		_cryptoMarketDataProvider = cryptoMarketDataProvider;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		_logger.LogInformation("Bitcoin Price Worker started.");

		while (!stoppingToken.IsCancellationRequested) {
			try {
				await UpdateBitcoinPriceAsync(stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
				break;
			}
			catch (Exception ex) {
				_logger.LogError(ex, "Error retrieving Bitcoin price.");
			}

			await Task.Delay(_interval, stoppingToken);
		}

		_logger.LogInformation("Bitcoin Price Worker stopped.");
	}

	private async Task UpdateBitcoinPriceAsync(CancellationToken ct) {
		// Chame aqui o método que criamos no ICryptoMarketDataProvider.
		// Exemplo:
		//
		// var price = await _cryptoMarketDataProvider.GetPriceAsync(..., ct);
		// _logger.LogInformation("BTC price: {Price}", price);
	}
}