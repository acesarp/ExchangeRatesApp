using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ExchangeRates.Server;

public static class AuthenticationConfiguration {
	public static IServiceCollection AddExchangeRatesAuthentication(this IServiceCollection services, IConfiguration configuration) {

		services.AddAuthentication(o => {
			o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			o.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			o.DefaultChallengeScheme = "oidc";
		})
		//.AddOAuth("OAuth", o => o.CallbackPath = "/oauth/callback")
		.AddCookie(o => o.Cookie.SecurePolicy = CookieSecurePolicy.Always)
		.AddOpenIdConnect("oidc", o => {
			o.Authority = configuration["IdentityServer:Authority"];
			o.ClientId = configuration["IdentityServer:ClientId"];
			o.ClientSecret = configuration["IdentityServer:ClientSecret"];
			o.ResponseType = "code";
			o.SaveTokens = true;
			o.GetClaimsFromUserInfoEndpoint = false;
			o.Scope.Add("openid");
			o.Scope.Add("profile");
			o.Scope.Add("email");
			o.Scope.Add($"api://{configuration["IdentityServer:ClientId"]}/access_as_user");
			o.TokenValidationParameters.NameClaimType = "name";
			//o.Scope.Add("access_as_user");

			// Behind IIS + nginx only /exchangerates/api/* reaches the server, so the callbacks must live under /api.
			o.CallbackPath = "/api/signin-oidc";
			o.SignedOutCallbackPath = "/api/signout-callback-oidc";

			// The proxy strips the /exchangerates prefix and the public host, so cookies need path "/" and must be
			// marked Secure explicitly (the server only sees plain http).
			o.CorrelationCookie.Path = "/";
			o.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
			o.NonceCookie.Path = "/";
			o.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

			var publicBaseUrl = configuration["IdentityServer:PublicBaseUrl"]?.TrimEnd('/');
			if (!string.IsNullOrWhiteSpace(publicBaseUrl)) {
				o.Events.OnRedirectToIdentityProvider = context => {
					context.ProtocolMessage.RedirectUri = publicBaseUrl + o.CallbackPath;
					return Task.CompletedTask;
				};
				o.Events.OnRedirectToIdentityProviderForSignOut = context => {
					context.ProtocolMessage.PostLogoutRedirectUri = publicBaseUrl + o.SignedOutCallbackPath;
					return Task.CompletedTask;
				};
			}
		})
		.AddJwtBearer(o => {
			o.Authority = configuration["IdentityServer:Authority"];
			o.Audience = configuration["IdentityServer:ClientId"];
			o.RequireHttpsMetadata = true;
			o.MapInboundClaims = false;
			o.TokenValidationParameters.NameClaimType = "name";
			o.Events = new JwtBearerEvents {
				OnMessageReceived = context => {
					var accessToken = context.Request.Query["access_token"];
					var path = context.HttpContext.Request.Path;

					if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs")) {
						context.Token = accessToken;
					}

					return Task.CompletedTask;
				}
			};
		});

		Console.WriteLine($"Authority: {configuration["IdentityServer:Authority"]}");
		Console.WriteLine($"ClientId: {configuration["IdentityServer:ClientId"]}");

		AddExchangeRatesAuthorization(services);

		return services;
	}
	private static IServiceCollection AddExchangeRatesAuthorization(this IServiceCollection services) {
		services.AddAuthorizationBuilder()
						.AddPolicy("ApiScope", policy => {
							policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
							policy.RequireAuthenticatedUser();
							policy.RequireClaim("scp", "access_as_user");
						});

		return services;
	}
}