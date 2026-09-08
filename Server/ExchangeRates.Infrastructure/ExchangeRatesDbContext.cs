using ExchangeRates.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Infrastructure;

public sealed class ExchangeRatesDbContext : DbContext {
	public ExchangeRatesDbContext(DbContextOptions<ExchangeRatesDbContext> options)
		: base(options) { }

	public DbSet<ExchangeRateEntity> ExchangeRates => Set<ExchangeRateEntity>();
	public DbSet<CurrencyEntity> Currencies => Set<CurrencyEntity>();
	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExchangeRatesDbContext).Assembly);
		modelBuilder.Entity<ExchangeRateEntity>(entity => {
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Rate).HasPrecision(28, 12);
			entity.Ignore(x => x.Provider);
			entity.HasIndex(x => new { x.Date, x.BaseCurrency, x.QuoteCurrency }).IsUnique();

		});

		modelBuilder.Entity<CurrencyEntity>(entity => {
			entity.ToTable("Currency");
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Code).HasMaxLength(3).IsRequired();
			entity.Property(x => x.NumericCode).IsRequired();
			entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
			entity.Property(x => x.IsHistoric).IsRequired();
			entity.Property(x => x.Priority).IsRequired();
			entity.HasIndex(x => x.Code).IsUnique();
			entity.HasIndex(x => x.Priority).IsUnique();
		});
	}
}