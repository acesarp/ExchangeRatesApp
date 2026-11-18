using ExchangeRates.Infrastructure;

using System.Diagnostics;
using System.Security.Claims;

namespace ExchangeRates.Server.Middleware;

public sealed class AccessLogMiddleware(RequestDelegate next, ILogger<AccessLogMiddleware> logger) {
	public async Task InvokeAsync(HttpContext ctx, ExchangeRatesDbContext db) {
		var path = ctx.Request.Path;
		if (!path.StartsWithSegments("/api")) { // skip hubs, swagger, static files
			await next(ctx);
			return;
		}

		var sw = Stopwatch.StartNew();
		try {
			await next(ctx);
		}
		finally {
			sw.Stop();
			// Read the user AFTER next(): the ApiScope policy authenticates the JWT during authorization
			var u = ctx.User;
			string? Claim(params string[] types) => types.Select(t => u.FindFirst(t)?.Value)
																					.FirstOrDefault(v => v is not null);

			db.AccessLogs.Add(new AccessLogEntity {
				TimestampUtc = DateTime.UtcNow,
				UserId = Claim("oid", "http://schemas.microsoft.com/identity/claims/objectidentifier"),
				UserName = u.Identity?.IsAuthenticated == true ? u.Identity.Name : null,
				Email = Claim("preferred_username", "email", ClaimTypes.Email),
				IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
				Method = ctx.Request.Method,
				Path = path.Value ?? "",
				QueryString = ctx.Request.QueryString.Value,
				StatusCode = ctx.Response.StatusCode,
				DurationMs = (int)sw.ElapsedMilliseconds,
				UserAgent = ctx.Request.Headers.UserAgent.ToString()
			});
			try {
				await db.SaveChangesAsync(CancellationToken.None);
			}
			catch (Exception ex) { // never break the request over logging
				logger.LogWarning(ex, "Failed to write access log");
			}
		}
	}
}