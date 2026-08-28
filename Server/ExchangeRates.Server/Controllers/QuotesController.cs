using ExchangeRates.Domain.Enums;
using ExchangeRates.Server.Interfaces;

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
	public async Task<ActionResult<IReadOnlyList<ExchangeRateResult>>> GetRates(ECurrencyISO baseCurrency, ECurrencyISO quoteCurrency, DateOnly? fromDate, DateOnly? toDate) {
		_logger.LogInformation("GetRate request: {From}->{To}, fromDate={FromDate}, toDate={ToDate}", baseCurrency, quoteCurrency, fromDate, toDate);

		var _fromDate = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
		var _toDate = toDate ?? _fromDate;

		try {
			var result = await _service.GetRatesAsync(baseCurrency, quoteCurrency, _fromDate, _toDate);
			_logger.LogInformation("GetRate response count: {Count} for {From}->{To}", result.Count, baseCurrency, quoteCurrency);
			return Ok(result);
		}
		catch (InvalidOperationException ex) {
			_logger.LogWarning(ex, "Unable to retrieve rates for {From}->{To}", baseCurrency, quoteCurrency);
			return BadRequest(new { error = ex.Message });
		}
	}

	[Route("available-currencies", Name = "AvailableCurrencies")]
	[HttpGet]
	public ActionResult<List<string>> GetAvailableCurrencies() {
		var currencies = Enum.GetNames<ECurrencyISO>()
														.OrderBy(o => o)
														.ToList();
		var result = Ok(currencies);
		return result;
	}
}
