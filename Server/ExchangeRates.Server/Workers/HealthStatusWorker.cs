using ExchangeRates.Server.Hubs;
using ExchangeRates.Server.Interfaces;

using Microsoft.AspNetCore.SignalR;

namespace ExchangeRates.Server.Services;

public sealed class HealthStatusWorker : BackgroundService {
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IHubContext<HealthStatusHub> _hubContext;
	private readonly ILogger<HealthStatusWorker> _logger;

	public HealthStatusWorker(IServiceScopeFactory scopeFactory, IHubContext<HealthStatusHub> hubContext, ILogger<HealthStatusWorker> logger) {
		_scopeFactory = scopeFactory;
		_hubContext = hubContext;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken ct) {

		while (!ct.IsCancellationRequested) {
			try {
				using var scope = _scopeFactory.CreateScope();

				var healthService = scope.ServiceProvider.GetRequiredService<IHealthStatusService>();
				var health = await healthService.GetDbHealthAsync(ct);

				await _hubContext.Clients.All.SendAsync("HealthStatusChanged", health, ct);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested) {
				_logger.LogInformation("Health status broadcasting was canceled.");
				break;
			}
			catch (Exception ex) {
				_logger.LogError(ex, "Error broadcasting health status");
			}

			await Task.Delay(TimeSpan.FromSeconds(10), ct);
		}
	}
}