using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Domain.Models;
using ExchangeRates.Server.Hubs;

using Microsoft.AspNetCore.SignalR;

namespace ExchangeRates.Server.Workers;

public sealed class BitcoinPriceWorker : BackgroundService {
	private readonly ICryptoMarketDataProvider _cryptoMarketDataProvider;
	private readonly IHubContext<CryptoPriceHub> _hubContext;
	private readonly ILogger<BitcoinPriceWorker> _logger;

	public BitcoinPriceWorker(ICryptoMarketDataProvider cryptoMarketDataProvider, IHubContext<CryptoPriceHub> hubContext, ILogger<BitcoinPriceWorker> logger) {
		_cryptoMarketDataProvider = cryptoMarketDataProvider;
		_hubContext = hubContext;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken ct) {
		_logger.LogInformation("Bitcoin Price Worker started using {Provider}.", _cryptoMarketDataProvider.Name);

		while (!ct.IsCancellationRequested) {
			try {
				await _cryptoMarketDataProvider.ConnectAsync(new string[] { "BTC-USD" }, OnPriceReceivedAsync, ct);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested) {
				_logger.LogInformation("Bitcoin price broadcasting was canceled.");
			}
			catch (Exception ex) {
				_logger.LogError(ex, "Bitcoin market data connection failed.");
			}

			_logger.LogInformation("Bitcoin Price Worker stopped.");
		}
	}

	private async Task OnPriceReceivedAsync(CryptoPriceResult price, CancellationToken ct) {
		_logger.LogInformation("BTC price update: {@Price}", price);
		await _hubContext.Clients.All.SendAsync("CryptoPriceUpdated", price, ct);
	}
}