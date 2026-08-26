using ExchangeRates.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Infrastructure;

public sealed class ExchangeRatesDbContext : DbContext {
	public ExchangeRatesDbContext(DbContextOptions<ExchangeRatesDbContext> options)
		: base(options) { }

	public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
	public DbSet<ExchangeRateFetch> ExchangeRateFetches => Set<ExchangeRateFetch>();

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExchangeRatesDbContext).Assembly);
	}
}