
using ExchangeRates.Server.Configuration;
using ExchangeRates.Server.Services;

namespace ExchangeRates.Server;

public class Program {
	public static void Main(string[] args) {
		var builder = WebApplication.CreateBuilder(args);
		builder.Services.Configure<BcbApiOptions>(builder.Configuration.GetSection("BcbApi"));
		// Add services to the container.

		builder.Services.AddControllers();
		// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
		builder.Services.AddOpenApi();
		builder.Services.AddHttpClient();
		builder.Services.AddScoped<BcbService>();
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
