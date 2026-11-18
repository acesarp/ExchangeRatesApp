using ExchangeRates.Infrastructure;

using MaxMind.GeoIP2;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;

using UAParser;
namespace ExchangeRates.Server.Middleware;

public sealed class AccessLogMiddleware(RequestDelegate next, ILogger<AccessLogMiddleware> logger) {
	public async Task InvokeAsync(HttpContext ctx, ExchangeRatesDbContext db, DatabaseReader geo, Parser uaParser) {
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
			var ip = ctx.Connection.RemoteIpAddress;
			if (ip is { IsIPv4MappedToIPv6: true }) {
				ip = ip.MapToIPv4();
			}

			string? country = null;
			string? region = null;
			string? city = null;
			double? lat = null;
			double? lon = null;

			if (ip is not null && !IsPrivate(ip) && geo.TryCity(ip, out var r) && r is not null) {
				country = r.Country.IsoCode;
				region = r.MostSpecificSubdivision.Name;
				city = r.City.Name;
				lat = r.Location.Latitude;
				lon = r.Location.Longitude;
			}

			var ua = ctx.Request.Headers.UserAgent.ToString();
			var c = string.IsNullOrEmpty(ua) ? null : uaParser.Parse(ua);

			db.AccessLogs.Add(new AccessLogEntity {
				TimestampUtc = DateTime.UtcNow,
				UserId = Claim("oid", "http://schemas.microsoft.com/identity/claims/objectidentifier"),
				UserName = u.Identity?.IsAuthenticated == true ? u.Identity.Name : null,
				Email = Claim("preferred_username", "email", ClaimTypes.Email),
				IpAddress = ip?.ToString(),
				Country = country,
				Region = region,
				City = city,
				Latitude = lat,
				Longitude = lon,
				Method = ctx.Request.Method,
				Path = path.Value ?? "",
				QueryString = ctx.Request.QueryString.Value,
				StatusCode = ctx.Response.StatusCode,
				DurationMs = (int)sw.ElapsedMilliseconds,
				UserAgent = ctx.Request.Headers.UserAgent.ToString(),
				Browser = c?.UA.Family,
				BrowserVersion = c?.UA.Major,
				OS = c?.OS.Family,
				Device = c?.Device.Family,
				IsBot = c?.Device.Family == "Spider" || string.IsNullOrEmpty(ua)
			});

			try {
				await db.SaveChangesAsync(CancellationToken.None);
			}
			catch (Exception ex) { // never break the request over logging
				logger.LogWarning(ex, "Failed to write access log");
			}
		}

	}

	static bool IsPrivate(IPAddress a) {
		if (IPAddress.IsLoopback(a)) {
			return true;
		}

		var b = a.GetAddressBytes();
		return a.AddressFamily == AddressFamily.InterNetwork &&
			(b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168));
	}
}