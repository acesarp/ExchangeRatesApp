using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Models;

using Microsoft.AspNetCore.Mvc;

namespace ExchangeRates.Server.Controllers;

[ApiController]
[Route("/api")]
public class QuotesController : ControllerBase {
	private readonly IExchangeRateService _service;
	private readonly ILogger<QuotesController> _logger;

	public QuotesController(IExchangeRateService service, ILogger<QuotesController> logger) {
		_service = service;
		_logger = logger;
	}

	[Route("exchange-rates", Name = "ExchangeRates")]
	[HttpGet]
	public async Task<ActionResult<IReadOnlyList<ExchangeRateResult>>> GetRates(string baseCurrency, string quoteCurrency, DateOnly? fromDate, DateOnly? toDate) {
		_logger.LogInformation($"GetRate request: {baseCurrency}->{quoteCurrency}, fromDate={fromDate}, toDate={toDate}");

		var _fromDate = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
		var _toDate = toDate ?? _fromDate;

		try {
			var result = await _service.GetRatesAsync(baseCurrency, quoteCurrency, _fromDate, _toDate);
			_logger.LogInformation($"GetRate response count: {result.Count} for {baseCurrency}->{quoteCurrency}");
			return Ok(result);
		}
		catch (InvalidOperationException ex) {
			_logger.LogError(ex, $"Unable to retrieve rates for {baseCurrency}->{quoteCurrency}");
			return BadRequest(new { error = ex.Message });
		}
	}

	[Route("available-currencies", Name = "AvailableCurrencies")]
	[HttpGet]
	public async Task<ActionResult<List<CurrencyModel>>> GetAvailableCurrencies() {
		var currencies = await _service.GetCurrenciesAsync();
		return Ok(currencies);

	}


	/// <summary> Returns Zacarias picture a audio file </summary>
	[Route("zaca-media", Name = "ZacaMedia")]
	[HttpGet]
	public ActionResult GetZacaMedia() {
		var picture = System.IO.File.ReadAllBytes("./Assets/zaca01.PNG");
		var audio = System.IO.File.ReadAllBytes("./Assets/risada-zacarias.mp3");

		var result = Ok(new { picture, audio });
		return result;
	}

	/// <summary> Returns the current hosting environment name </summary>
	[HttpGet("environment")]
	public IActionResult GetEnvironment([FromServices] IWebHostEnvironment environment) {
		return Ok(new { environment = environment.EnvironmentName });
	}
}
