using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Domain.Models;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ExchangeRates.Infrastructure.Providers.Crypto;

public sealed class KrakenMarketDataProvider : ICryptoMarketDataProvider {
	private const string WebSocketUrl = "wss://ws.kraken.com/v2";

	public string Name => "Kraken";

	public async Task ConnectAsync(IEnumerable<string> symbols, Func<CryptoPriceResult, CancellationToken, Task> onPriceReceived, CancellationToken ct) {
		using var socket = new ClientWebSocket();

		await socket.ConnectAsync(new Uri(WebSocketUrl), ct);
		await SubscribeAsync(socket, symbols, ct);
		await ReceiveAsync(socket, onPriceReceived, ct);
	}

	private static async Task SubscribeAsync(ClientWebSocket socket, IEnumerable<string> symbols, CancellationToken ct) {
		var message = new {
			method = "subscribe",
			@params = new {
				channel = "ticker",
				symbol = symbols.ToArray()
			}
		};

		var json = JsonSerializer.Serialize(message);
		var bytes = Encoding.UTF8.GetBytes(json);

		await socket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
	}

	private async Task ReceiveAsync(ClientWebSocket socket, Func<CryptoPriceResult, CancellationToken, Task> onPriceReceived, CancellationToken ct) {
		var buffer = new byte[8192];

		while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested) {
			using var stream = new MemoryStream();

			WebSocketReceiveResult result;

			do {
				result = await socket.ReceiveAsync(buffer, ct);

				if (result.MessageType == WebSocketMessageType.Close) {
					await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
					return;
				}

				stream.Write(buffer, 0, result.Count);
			}
			while (!result.EndOfMessage);

			if (result.MessageType != WebSocketMessageType.Text) {
				continue;
			}

			var json = Encoding.UTF8.GetString(stream.ToArray());

			await ProcessMessageAsync(json, onPriceReceived, ct);
		}
	}

	private async Task ProcessMessageAsync(string json, Func<CryptoPriceResult, CancellationToken, Task> onPriceReceived, CancellationToken ct) {
		using var document = JsonDocument.Parse(json);

		var root = document.RootElement;

		if (!root.TryGetProperty("channel", out var channel)) {
			return;
		}

		if (!string.Equals(channel.GetString(), "ticker", StringComparison.OrdinalIgnoreCase)) {
			return;
		}

		if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) {
			return;
		}

		foreach (var ticker in data.EnumerateArray()) {
			if (!ticker.TryGetProperty("symbol", out var symbolElement) || !ticker.TryGetProperty("last", out var lastElement)) {
				continue;
			}

			var symbol = symbolElement.GetString();

			if (string.IsNullOrWhiteSpace(symbol)) {
				continue;
			}

			var price = lastElement.GetDecimal();
			var result = new CryptoPriceResult(symbol, price, DateTime.UtcNow, Name);

			await onPriceReceived(result, ct);
		}
	}


}