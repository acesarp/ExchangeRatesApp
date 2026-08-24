using ExchangeRates.Server.Enums;
using ExchangeRates.Server.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace ExchangeRates.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class QuotesController : ControllerBase {
	private readonly IExchangeRateService _service;

	public QuotesController(IExchangeRateService service) {
		_service = service;
	}
	[HttpGet(Name = "GetExchangeRate")]
	public async Task<ActionResult<Server.ExchangeRate>> GetRate(ECurrency fromCurrency, ECurrency toCurrency, DateOnly? date) {
		var result = await _service.GetRatesAsync(fromCurrency, toCurrency, date);
		return Ok(result);
	}
}
