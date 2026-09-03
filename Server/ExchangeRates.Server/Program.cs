using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Infrastructure;
using ExchangeRates.Infrastructure.Repositories;
using ExchangeRates.Server.Configuration;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;
using ExchangeRates.Server.Services;

using Microsoft.EntityFrameworkCore;

using Serilog;

using System.Text.Json.Serialization;

namespace ExchangeRates.Server;

public class Program {
	public static void Main(string[] args) {
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.Enrich.FromLogContext()
			.WriteTo.Console()
			.WriteTo.File("logs/log-.log", rollingInterval: RollingInterval.Day)
			.CreateLogger();
		try {
			var builder = WebApplication.CreateBuilder(args);
			builder.Host.UseSerilog();

			Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);

			builder.Configuration.AddJsonFile("providerkeys.json", optional: false, reloadOnChange: true);

			builder.Services.Configure<Dictionary<string, string>>(builder.Configuration.GetSection("ProviderKeys"));
			builder.Services.Configure<CentralBankOptions>(builder.Configuration.GetSection("CentralBanks"));

			// Add services to the container.
			builder.Services.AddDbContext<ExchangeRatesDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ExchangeRates")));

			Log.Logger.Information("Connection string: {ConnectionString}", builder.Configuration.GetConnectionString("ExchangeRates"));

			builder.Services.AddControllers()
				.AddJsonOptions(options => {
					options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
				});
			// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
			builder.Services.AddOpenApi();
			builder.Services.AddHttpClient();

			builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
			builder.Services.AddSingleton<FixedExchangeRateProvider>();
			builder.Services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();

			builder.Services.AddCentralBankProviders();

			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			builder.Services.AddCors(ob => {
				ob.AddPolicy("BlazorClient", policy => {
					policy.WithOrigins("https://localhost:7149", "http://localhost:62866", "https://localhost:62866")
						   .AllowAnyMethod()
						   .AllowAnyHeader();
				});
			});

			var app = builder.Build();
			app.UseCors("BlazorClient");

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
		catch (Exception ex) {
			Log.Fatal(ex, "Host terminated unexpectedly");
			throw;
		}
		finally {
			Log.CloseAndFlush();
		}
	}
}
