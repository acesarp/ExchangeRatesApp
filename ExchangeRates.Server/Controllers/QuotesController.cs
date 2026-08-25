using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRates.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class QuotesController : ControllerBase {
	private readonly IExchangeRateService _service;
	private readonly ILogger<QuotesController> _logger;

	public QuotesController(IExchangeRateService service, ILogger<QuotesController> logger) {
		_service = service;
		_logger = logger;
	}

	[HttpGet(Name = "GetExchangeRate")]
	public async Task<ActionResult<IReadOnlyList<ExchangeRate>>> GetRate(ECurrencyISO fromCurrency, ECurrencyISO toCurrency, DateOnly? fromDate, DateOnly? toDate) {
		_logger.LogInformation("GetRate request: {From}->{To}, fromDate={FromDate}, toDate={ToDate}", fromCurrency, toCurrency, fromDate, toDate);
		try {
			var result = await _service.GetRatesAsync(fromCurrency, toCurrency, fromDate, toDate);
			_logger.LogInformation("GetRate response count: {Count} for {From}->{To}", result.Count, fromCurrency, toCurrency);
			return Ok(result);
		}
		catch (InvalidOperationException ex) {
			_logger.LogWarning(ex, "Unable to retrieve rates for {From}->{To}", fromCurrency, toCurrency);
			return BadRequest(new { error = ex.Message });
		}
	}
}
