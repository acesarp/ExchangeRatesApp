using Microsoft.AspNetCore.Mvc;

namespace ExchangeRates.Server.Controllers;


[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase {

	//[Produces("application/json")]
	//[ApiExplorerSettings(GroupName = "Admin")]
	[HttpPost("login")]
	public async Task<IActionResult> Login() {
		return Ok("Admin access granted.");
	}

	[HttpGet("secret-data")]
	public IActionResult SecretData() {
		return Ok("This is secret data accessible only to Admins.");
	}

	[HttpGet("info")]
	public IActionResult AdminInfo() {
		return Ok("Admin information: You have the necessary privileges to access this endpoint.");
	}

	[HttpGet("dashboard")]
	public IActionResult AdminDashboard() {
		return Ok("Welcome to the Admin Dashboard. You have full access to administrative features.");
	}



}
