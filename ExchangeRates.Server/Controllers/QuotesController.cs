using ExchangeRates.Domain.Enums;
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
	public async Task<ActionResult<IReadOnlyList<ExchangeRateResult>>> GetRate(ECurrencyISO quoteCurrency, ECurrencyISO toCurrency, DateOnly? fromDate, DateOnly? toDate) {
		_logger.LogInformation("GetRate request: {From}->{To}, fromDate={FromDate}, toDate={ToDate}", quoteCurrency, toCurrency, fromDate, toDate);

		var _fromDate = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
		var _toDate = toDate ?? _fromDate;

		try {
			var result = await _service.GetRatesAsync(quoteCurrency, toCurrency, _fromDate, _toDate);
			_logger.LogInformation("GetRate response count: {Count} for {From}->{To}", result.Count, quoteCurrency, toCurrency);
			return Ok(result);
		}
		catch (InvalidOperationException ex) {
			_logger.LogWarning(ex, "Unable to retrieve rates for {From}->{To}", quoteCurrency, toCurrency);
			return BadRequest(new { error = ex.Message });
		}
	}
}
