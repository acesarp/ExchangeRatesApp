using ExchangeRates.Domain.Models;
using ExchangeRates.Server.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace ExchangeRates.Server.Controllers;

[ApiController]
[Route("api/")]
public class QuotesController : ControllerBase {
	private readonly IExchangeRateService _service;
	private readonly IHealthStatusService _HealthCheckService;
	private readonly ILogger<QuotesController> _logger;

	public QuotesController(IExchangeRateService service, IHealthStatusService healthCheckService, ILogger<QuotesController> logger) {
		_service = service;
		_HealthCheckService = healthCheckService;
		_logger = logger;
	}

	[HttpGet("exchange-rates", Name = "ExchangeRates")]
	public async Task<ActionResult<IReadOnlyList<ExchangeRateResult>>> ExchangeRates(string baseCurrency = "jpy", string quoteCurrency = "EUR", DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default) {
		baseCurrency = baseCurrency.ToUpperInvariant();
		quoteCurrency = quoteCurrency.ToUpperInvariant();

		fromDate ??= new DateOnly(2026, 09, 20);
		toDate ??= new DateOnly(2026, 09, 22);
		_logger.LogInformation($"ExchangeRates request: {baseCurrency}->{quoteCurrency}, fromDate={fromDate}, toDate={toDate}");

		var _fromDate = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
		var _toDate = toDate ?? _fromDate;

		try {
			var result = await _service.GetRatesAsync(baseCurrency, quoteCurrency, _fromDate, _toDate, ct);
			_logger.LogInformation($"GetRate response count: {result.Count} for {baseCurrency}->{quoteCurrency}");
			return Ok(result);
		}
		catch (ArgumentException ex) {
			return BadRequest(new { error = ex.Message });
		}
		catch (InvalidOperationException ex) {
			_logger.LogError(ex, $"Unable to retrieve rates for {baseCurrency}->{quoteCurrency}");
			return BadRequest(new { error = ex.Message });
		}
	}

	[HttpGet("available-currencies", Name = "AvailableCurrencies")]
	public async Task<ActionResult<List<CurrencyModel>>> AvailableCurrencies(CancellationToken ct) {
		var currencies = await _service.GetCurrenciesAsync(ct);
		return Ok(currencies);

	}


	/// <summary> Returns Zacarias picture a audio file </summary>
	[HttpGet("zaca-media", Name = "ZacaMedia")]
	public ActionResult ZacaMedia() {
		var picture = System.IO.File.ReadAllBytes("./Assets/zaca01.PNG");
		var audio = System.IO.File.ReadAllBytes("./Assets/risada-zacarias.mp3");

		var result = Ok(new { picture, audio });
		return result;
	}

	/// <summary> Returns the current hosting environment name </summary>
	[HttpGet("environment")]
	public IActionResult Environment([FromServices] IWebHostEnvironment environment) {
		return Ok(new { environment = environment.EnvironmentName });
	}

}
