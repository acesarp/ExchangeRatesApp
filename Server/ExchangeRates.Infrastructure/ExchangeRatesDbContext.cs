using ExchangeRates.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Infrastructure;

public sealed class ExchangeRatesDbContext : DbContext {
	public ExchangeRatesDbContext(DbContextOptions<ExchangeRatesDbContext> options) : base(options) { }

	public DbSet<ExchangeRateEntity> ExchangeRates => Set<ExchangeRateEntity>();
	public DbSet<CurrencyEntity> Currencies => Set<CurrencyEntity>();
	public DbSet<CentralBankEntity> CentralBanks => Set<CentralBankEntity>();
	public DbSet<FixedExchangeRateEntity> FixedExchangeRates => Set<FixedExchangeRateEntity>();
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

		modelBuilder.Entity<CentralBankEntity>(entity => {
			entity.ToTable("CentralBank");

			entity.HasKey(x => x.Id);

			entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
			entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
			entity.Property(x => x.IsActive).IsRequired();
			entity.Property(x => x.CreatedAtUtc).IsRequired();
			entity.Property(x => x.CountryOfOrigin).HasMaxLength(100).IsRequired(false);
			entity.HasIndex(x => x.Code).IsUnique();

			entity.HasIndex(x => x.Priority)
				.IsUnique()
				.HasFilter("[Priority] IS NOT NULL");

			entity.HasOne(x => x.Currency)
				.WithMany()
				.HasForeignKey(x => x.CurrencyId)
				.OnDelete(DeleteBehavior.Restrict);
		});


		modelBuilder.Entity<FixedExchangeRateEntity>(entity => {
			entity.ToTable("FixedExchangeRate");
			entity.HasKey(x => x.Id);
			entity.ToTable(t => {
				t.HasCheckConstraint("CK_FixedExchangeRate_Rate", "[Rate] > 0");
				t.HasCheckConstraint("CK_FixedExchangeRate_DifferentCurrencies", "[CurrencyId] <> [PeggedOnCurrencyId]");
				t.HasCheckConstraint("CK_FixedExchangeRate_ValidPeriod", "[ValidTo] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] >= [ValidFrom]");
			});

			entity.Property(x => x.Rate).HasPrecision(28, 12).IsRequired();
			entity.Property(x => x.IsActive).IsRequired();
			entity.Property(x => x.ValidFrom).HasColumnType("date").IsRequired(false);
			entity.Property(x => x.ValidTo).HasColumnType("date").IsRequired(false);

			entity.HasIndex(x => x.CurrencyId);
			entity.HasIndex(x => new { x.CurrencyId, x.ValidFrom, x.ValidTo });

			entity.HasOne(x => x.Currency)
				.WithMany()
				.HasForeignKey(x => x.CurrencyId)
				.OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(x => x.PeggedOnCurrency)
				.WithMany()
				.HasForeignKey(x => x.PeggedOnCurrencyId)
				.OnDelete(DeleteBehavior.Restrict);
		});
	}
}