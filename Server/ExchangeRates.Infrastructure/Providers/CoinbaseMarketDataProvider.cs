using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Domain.Models;

using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExchangeRates.Infrastructure.Providers;

public sealed class CoinbaseMarketDataProvider : ICryptoMarketDataProvider {
	private const string WebSocketUrl = "wss://advanced-trade-ws.coinbase.com";
	public string Name => "Coinbase";

	public async Task ConnectAsync(IEnumerable<string> symbols, Func<CryptoPriceResult, CancellationToken, Task> onPriceReceived, CancellationToken ct) {
		ArgumentNullException.ThrowIfNull(symbols);
		ArgumentNullException.ThrowIfNull(onPriceReceived);

		var productIds = symbols.Where(x => !string.IsNullOrWhiteSpace(x))
											.Select(NormalizeSymbol)
											.Distinct(StringComparer.OrdinalIgnoreCase)
											.ToArray();

		using var socket = new ClientWebSocket();

		await socket.ConnectAsync(new Uri(WebSocketUrl), ct);

		await SubscribeAsync(socket, productIds, ct);
		await ReceiveAsync(socket, onPriceReceived, ct);
	}

	private static string NormalizeSymbol(string symbol) {
		symbol = symbol.Trim().ToUpperInvariant();

		if (symbol.Contains('-')) {
			return symbol;
		}

		if (symbol.Contains('/')) {
			return symbol.Replace('/', '-');
		}

		return $"{symbol}-USD";
	}

	private static async Task SubscribeAsync(ClientWebSocket socket, string[] productIds, CancellationToken ct) {
		var subscription = new {
			type = "subscribe",
			product_ids = productIds,
			channel = "ticker"
		};

		var json = JsonSerializer.Serialize(subscription);
		var buffer = Encoding.UTF8.GetBytes(json);

		await socket.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
	}

	private static async Task ReceiveAsync(ClientWebSocket socket, Func<CryptoPriceResult, CancellationToken, Task> onPriceReceived, CancellationToken ct) {
		var buffer = new byte[16 * 1024];

		while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open) {
			using var message = new MemoryStream();

			WebSocketReceiveResult result;

			do {
				result = await socket.ReceiveAsync(buffer, ct);

				if (result.MessageType == WebSocketMessageType.Close) {
					await CloseAsync(socket, ct);
					return;
				}

				message.Write(buffer, 0, result.Count);
			}
			while (!result.EndOfMessage);

			if (result.MessageType != WebSocketMessageType.Text) {
				continue;
			}

			message.Position = 0;

			using var document = await JsonDocument.ParseAsync(message, cancellationToken: ct);

			//Console.WriteLine(document.RootElement.ToString());
			//var d = document.Deserialize<string>();

			try {
				var j = Encoding.UTF8.GetString(message.ToArray(), 0, message.ToArray().Length);
				Console.WriteLine(j);
				message.Position = 0;
				var response = JsonSerializer.Deserialize<Response>(message, new JsonSerializerOptions {
					NumberHandling = JsonNumberHandling.AllowReadingFromString,
					IncludeFields = true,
					PropertyNameCaseInsensitive = true
				});
			}
			catch (JsonException ex) {
				Console.WriteLine($"Error deserializing message: {ex}");
			}

			//Console.WriteLine(response);

			await ProcessMessageAsync(document.RootElement, onPriceReceived, ct);
		}
	}

	private static async Task ProcessMessageAsync(JsonElement root, Func<CryptoPriceResult, CancellationToken, Task> onPriceReceived, CancellationToken ct) {
		if (!root.TryGetProperty("channel", out var channel) || channel.GetString() != "ticker") {
			return;
		}

		if (!root.TryGetProperty("events", out var events)) {
			return;
		}

		foreach (var @event in events.EnumerateArray()) {
			if (!@event.TryGetProperty("tickers", out var tickers)) {
				continue;
			}

			foreach (var ticker in tickers.EnumerateArray()) {
				var productId = ticker.GetProperty("product_id").GetString();
				var priceText = ticker.GetProperty("price").GetString();

				if (string.IsNullOrWhiteSpace(productId)) {
					continue;
				}

				if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price)) {
					continue;
				}

				var result = new CryptoPriceResult(productId, price, DateTime.UtcNow, "Coinbase");
				await onPriceReceived(result, ct);
			}
		}
	}

	private static async Task CloseAsync(ClientWebSocket socket, CancellationToken ct) {
		if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived) {
			await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing Coinbase connection.", ct);
		}
	}

	[JsonSerializable(typeof(Response))]
	private class Response {
		public string channel;
		public DateTime timestamp;
		public int sequence_num;
		public Event[] events;
	}

	[JsonSerializable(typeof(Event))]
	public class Event {
		public string type;
		public Ticker[] tickers;
	}
	[JsonSerializable(typeof(Ticker))]
	public class Ticker {
		public string type;
		public string product_id;
		public decimal price;
		public decimal volume_24_h;
		public decimal low_24_h;
		public decimal high_24_h;
		public decimal low_52_w;
		public decimal high_52_w;
		public decimal price_percent_chg_24_h;
		public decimal best_bid;
		public decimal best_ask;
		public decimal best_bid_quantity;
		public decimal best_ask_quantity;
	}

}
