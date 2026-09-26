using Microsoft.AspNetCore.Authentication.Cookies;

namespace ExchangeRates.Server;

public static class AuthenticationConfiguration {
	public static IServiceCollection AddExchangeRatesAuthentication(this IServiceCollection services, IConfiguration configuration) {

		services.AddAuthentication(o => {
			o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			o.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			o.DefaultChallengeScheme = "oidc";
		})
					.AddOAuth("OAuth", o => o.CallbackPath = "/oauth/callback")
					.AddCookie()
					.AddOpenIdConnect("oidc", o => {
						o.Authority = configuration["IdentityServer:Authority"];
						o.ClientId = configuration["IdentityServer:ClientId"];
						o.ClientSecret = configuration["IdentityServer:ClientSecret"];
						o.ResponseType = "code";
						o.SaveTokens = true;
						o.GetClaimsFromUserInfoEndpoint = true;
						o.Scope.Add("openid");
						o.Scope.Add("profile");
						o.Scope.Add("email");
						o.Scope.Add("ExchangeRatesAPI");
					})
			.AddJwtBearer(o => {
				o.Authority = configuration["IdentityServer:Authority"];
				o.RequireHttpsMetadata = true;
				o.Audience = "ExchangeRatesAPI";
			});

		AddExchangeRatesAuthorization(services);

		return services;
	}
	private static IServiceCollection AddExchangeRatesAuthorization(this IServiceCollection services) {
		services.AddAuthorizationBuilder()
						.AddPolicy("ApiScope", policy => {
							policy.RequireAuthenticatedUser();
							policy.RequireClaim("scope", "ExchangeRatesAPI");
						});

		return services;
	}
}