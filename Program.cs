
using ExchangeRates.Server.Configuration;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;
using ExchangeRates.Server.Services;

namespace ExchangeRates.Server;

public class Program {
	public static void Main(string[] args) {
		var builder = WebApplication.CreateBuilder(args);
		builder.Configuration.AddJsonFile("providerkeys.json", optional: false, reloadOnChange: false);
		builder.Services.Configure<Dictionary<string, string>>(builder.Configuration.GetSection("ProviderKeys"));

		builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
		builder.Services.Configure<CentralBankOptions>(builder.Configuration.GetSection("CentralBanks"));
		// Add services to the container.

		builder.Services.AddControllers();
		// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
		builder.Services.AddOpenApi();
		builder.Services.AddHttpClient();

		builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
		builder.Services.AddSingleton<FixedExchangeRateProvider>();
		builder.Services.AddCentralBankProviders();

		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment()) {
			app.MapOpenApi();
			app.UseSwagger();
			app.UseSwaggerUI(options => {
				options.SwaggerEndpoint("/swagger/v1/swagger.json", "FxRates API v1");
				options.RoutePrefix = string.Empty;
			});
		}

		app.UseHttpsRedirection();
		app.UseAuthorization();
		app.MapControllers();

		app.Run();
	}
}
