using ExchangeRates.Server.Models;
using ExchangeRates.Server.Services;

using Microsoft.AspNetCore.Mvc;

namespace ExchangeRates.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class QuotesController : ControllerBase {
	private readonly BcbService _bcbService;

	public QuotesController(BcbService bcbService) {
		_bcbService = bcbService;
	}
	[HttpGet(Name = "GetExchangeRate")]
	public async Task<ActionResult<BcbQuote>> GetRate([FromQuery] DateTime date, ICurrency currency) {
		var result = await _bcbService.GetQuoteAsync(date, currency);
		return Ok(result);
	}
}
