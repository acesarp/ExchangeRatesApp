using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRates.Server.Controllers;


[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase {
	// Behind the proxy "/" would land on the host root instead of the app, so return to the public base URL when configured.
	private readonly string _appRootUrl;

	public AdminController(IConfiguration configuration) {
		_appRootUrl = (configuration["IdentityServer:PublicBaseUrl"]?.TrimEnd('/') ?? string.Empty) + "/";
	}

	//[Produces("application/json")]
	//[ApiExplorerSettings(GroupName = "Admin")]
	[AllowAnonymous]
	[HttpGet("login")]
	public async Task<IActionResult> Login() {
		return Challenge(new AuthenticationProperties { RedirectUri = _appRootUrl }, "oidc");
	}

	[Authorize(Policy = "ApiScope")]
	[HttpGet("me")]
	public IActionResult Me() {
		return Ok(new {
			User.Identity?.IsAuthenticated,
			User.Identity?.Name,
			Claims = User.Claims.Select(c => new { c.Type, c.Value })
		});
	}

	[HttpGet("logout")]
	public IActionResult Logout() {
		return SignOut(new AuthenticationProperties { RedirectUri = _appRootUrl }, "Cookies", "oidc");
	}

	[Authorize(Policy = "ApiScope")]
	[HttpGet("secret-data")]
	public IActionResult SecretData() {
		return Ok("This is secret data accessible only to Admins.");
	}

	[Authorize(Policy = "ApiScope")]
	[HttpGet("info")]
	public IActionResult AdminInfo() {
		return Ok("Admin information: You have the necessary privileges to access this endpoint.");
	}

	[Authorize(Policy = "ApiScope")]
	[HttpGet("dashboard")]
	public IActionResult AdminDashboard() {
		return Ok("Welcome to the Admin Dashboard. You have full access to administrative features.");
	}

	[Authorize(Policy = "ApiScope")]
	[HttpGet("secure")]
	public IActionResult Secure() {
		return Ok(new {
			Message = "OAuth + JWT funcionando",
			User = User.Identity?.Name
		});
	}

	[Authorize]
	[HttpGet("token")]
	public async Task<IActionResult> Token() {
		var accessToken = await HttpContext.GetTokenAsync("access_token");

		return Ok(new {
			HasAccessToken = !string.IsNullOrEmpty(accessToken),
			AccessToken = accessToken
		});
	}

}
