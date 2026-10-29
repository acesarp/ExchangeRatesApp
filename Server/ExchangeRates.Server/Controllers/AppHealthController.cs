using ExchangeRates.Server.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace ExchangeRates.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AppHealthController : ControllerBase {
	private readonly IHealthStatusService _healthCheckService;
	private readonly ILogger<AppHealthController> _logger;

	public AppHealthController(IHealthStatusService healthCheckService, ILogger<AppHealthController> logger) {
		_healthCheckService = healthCheckService;
		_logger = logger;
	}

	[HttpGet("status-check")]
	public async Task<IActionResult> HealthStatus(CancellationToken ct) {
		try {
			var isHealthy = await _healthCheckService.GetDbHealthAsync(ct);
			if (isHealthy.CanConnect) {
				return Ok(new { isHealthy.CanConnect, isHealthy.LatencyMs, isHealthy.Error });
			}
			else {
				return StatusCode(503, new { isHealthy.CanConnect, isHealthy.LatencyMs, isHealthy.Error });
			}
		}
		catch (Exception ex) {
			_logger.LogError(ex, "Health check failed");
			return StatusCode(500, new { CanConnect = false, LatencyMs = 0D, error = ex.Message });
		}
	}
}
