using ExchangeRates.Domain.Interfaces;
using ExchangeRates.Infrastructure;
using ExchangeRates.Infrastructure.Repositories;
using ExchangeRates.Server.Configuration;
using ExchangeRates.Server.Interfaces;
using ExchangeRates.Server.Providers;
using ExchangeRates.Server.Services;

using Microsoft.EntityFrameworkCore;

using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

using System.Text.Json.Serialization;

namespace ExchangeRates.Server;

public class Program {
	public static void Main(string[] args) {
		try {
			var builder = WebApplication.CreateBuilder(args);
			var connectionString = builder.Configuration.GetConnectionString("ExchangeRates");

			SelfLog.Enable(message => Console.Error.WriteLine($"SERILOG SELFLOG: {message}"));

			var loggerConfiguration = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
				.MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
				.MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
				.MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
				.Enrich.FromLogContext()
				.WriteTo.Console()
				.WriteTo.File("logs/log-.log", rollingInterval: RollingInterval.Day);

			Exception? sqlSinkConfigurationException = null;
			if (!string.IsNullOrWhiteSpace(connectionString)) {
				var columnOptions = new ColumnOptions();
				columnOptions.Store.Clear();
				columnOptions.Store.Add(StandardColumn.Id);
				columnOptions.Store.Add(StandardColumn.Message);
				columnOptions.Store.Add(StandardColumn.MessageTemplate);
				columnOptions.Store.Add(StandardColumn.Level);
				columnOptions.Store.Add(StandardColumn.TimeStamp);
				columnOptions.Store.Add(StandardColumn.Exception);
				columnOptions.Store.Add(StandardColumn.Properties);
				columnOptions.Store.Add(StandardColumn.LogEvent);

				loggerConfiguration = loggerConfiguration.WriteTo.MSSqlServer(
					connectionString: connectionString,
					sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs", SchemaName = "dbo", AutoCreateSqlTable = true },
					columnOptions: columnOptions,
					restrictedToMinimumLevel: LogEventLevel.Debug);
			}

			Log.Logger = loggerConfiguration.CreateLogger();
			builder.Host.UseSerilog();

			Log.Information($"Environment: {builder.Environment.EnvironmentName}");
			Log.Information($"SQL Server sink enabled: {!string.IsNullOrWhiteSpace(connectionString)}");
			if (sqlSinkConfigurationException is not null) {
				Log.Warning(sqlSinkConfigurationException, "SQL Server sink could not be configured; continuing with console and file sinks.");
			}

			builder.Configuration.AddJsonFile("providerkeys.json", optional: false, reloadOnChange: true);

			builder.Services.Configure<Dictionary<string, string>>(builder.Configuration.GetSection("ProviderKeys"));
			builder.Services.Configure<CentralBankOptions>(builder.Configuration.GetSection("CentralBanks"));

			// Add services to the container.
			builder.Services.AddDbContext<ExchangeRatesDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ExchangeRates")));

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
			builder.Services.AddScoped<CentralBankProviderFactory>();

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
				//app.UseSwagger();
				//app.UseSwaggerUI(options => {
				//	options.SwaggerEndpoint("/swagger/v1/swagger.json", "FxRates API v1");
				//	options.RoutePrefix = string.Empty;
				//});
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
