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
	public async Task<ActionResult<Models.ExchangeRate>> GetRate([FromQuery] DateOnly date, string provider) {
		var result = await _service.GetRatesAsync(provider, date);
		return Ok(result);
	}
}
