using Microsoft.AspNetCore.SignalR;

using System.Collections.Concurrent;

namespace ExchangeRates.Server.Hubs;

public sealed class CryptoPriceHub : Hub {
	private static readonly ConcurrentDictionary<string, DateTime> Connections = new();

	public override async Task OnConnectedAsync() {
		Connections[Context.ConnectionId] = DateTime.UtcNow;

		Console.WriteLine($"Connected: {Context.ConnectionId}");
		Console.WriteLine($"Clients connected: {Connections.Count}");
		foreach (var item in Connections) {
			Console.WriteLine(item.Key + " connected at " + item.Value);
		}
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception) {
		Connections.TryRemove(Context.ConnectionId, out _);

		Console.WriteLine($"Disconnected: {Context.ConnectionId}");
		Console.WriteLine($"Clients connected: {Connections.Count}");

		await base.OnDisconnectedAsync(exception);
	}

	public static IReadOnlyCollection<string> GetConnections() => Connections.Keys.ToArray();
}