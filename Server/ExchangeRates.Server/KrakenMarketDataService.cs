namespace ExchangeRates.Server;

using ExchangeRates.Server.Interfaces;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public sealed class KrakenMarketDataService : IKrakenMarketDataService {
	private const string KrakenWebSocketUrl = "wss://ws.kraken.com/v2";

	private readonly ILogger<KrakenMarketDataService> _logger;

	public KrakenMarketDataService(ILogger<KrakenMarketDataService> logger) {
		_logger = logger;
	}

	public async Task ConnectAsync(CancellationToken ct) {
		using var socket = new ClientWebSocket();

		_logger.LogInformation("Connecting to Kraken WebSocket...");

		await socket.ConnectAsync(new Uri(KrakenWebSocketUrl), ct);

		_logger.LogInformation("Connected to Kraken.");

		await SubscribeAsync(socket, ct);
		await ReceiveAsync(socket, ct);
	}

	private static async Task SubscribeAsync(ClientWebSocket socket, CancellationToken ct) {
		var request = new {
			method = "subscribe",
			@params = new {
				channel = "ticker",
				symbol = new[] { "BTC/USD" }
			}
		};

		var json = JsonSerializer.Serialize(request);
		var buffer = Encoding.UTF8.GetBytes(json);

		await socket.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
	}

	private async Task ReceiveAsync(ClientWebSocket socket, CancellationToken ct) {
		var buffer = new byte[8192];

		while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open) {
			var result = await socket.ReceiveAsync(buffer, ct);

			if (result.MessageType == WebSocketMessageType.Close) {
				break;
			}

			var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

			_logger.LogInformation("Kraken: {Message}", json);
		}
	}
}