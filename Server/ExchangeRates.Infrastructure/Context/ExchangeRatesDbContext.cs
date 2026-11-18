using ExchangeRates.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Infrastructure;

public sealed class ExchangeRatesDbContext : DbContext {
	public ExchangeRatesDbContext(DbContextOptions<ExchangeRatesDbContext> options) : base(options) { }
	public DbSet<ExchangeRateEntity> ExchangeRates => Set<ExchangeRateEntity>();
	public DbSet<CurrencyEntity> Currencies => Set<CurrencyEntity>();
	public DbSet<CentralBankEntity> CentralBanks => Set<CentralBankEntity>();
	public DbSet<FixedExchangeRateEntity> FixedExchangeRates => Set<FixedExchangeRateEntity>();
	public DbSet<CentralBankSupportedCurrencyEntity> CentralBankSupportedCurrencies => Set<CentralBankSupportedCurrencyEntity>();
	public DbSet<ExchangeRateUnavailableDateEntity> ExchangeRateUnavailableDates => Set<ExchangeRateUnavailableDateEntity>();
	public DbSet<PreferredProviderEntity> PreferredProviders => Set<PreferredProviderEntity>();
	public DbSet<AccessLogEntity> AccessLogs => Set<AccessLogEntity>();
	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<AccessLogEntity>(e => {
			e.ToTable("AccessLog");
			e.HasIndex(x => x.TimestampUtc);
			e.HasIndex(x => x.UserId);
			e.Property(x => x.Path).HasMaxLength(512);
			e.Property(x => x.QueryString).HasMaxLength(1024);
			e.Property(x => x.UserAgent).HasMaxLength(512);
			e.Property(x => x.IpAddress).HasMaxLength(64);
			e.Property(x => x.City).HasMaxLength(64);
			e.Property(x => x.Country).HasMaxLength(128);
			e.Property(x => x.DurationMs).HasMaxLength(64);
			e.Property(x => x.Region).HasMaxLength(128);
			e.Property(x => x.Latitude).HasMaxLength(64);
			e.Property(x => x.Longitude).HasMaxLength(64);
		});

		modelBuilder.Entity<ExchangeRateEntity>(entity => {
			entity.ToTable("ExchangeRate");
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => new { x.Date, x.BaseCurrencyId, x.QuoteCurrencyId }).IsUnique();

			entity.Property(x => x.Rate).HasPrecision(28, 12);
			entity.Property(x => x.Date).HasColumnType("date").IsRequired();
			entity.HasOne(x => x.BaseCurrency).WithMany().HasForeignKey(x => x.BaseCurrencyId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(x => x.QuoteCurrency).WithMany().HasForeignKey(x => x.QuoteCurrencyId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<CurrencyEntity>(entity => {
			entity.ToTable("Currency");
			entity.HasKey(x => x.Id);

			entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
			entity.Property(x => x.NumericCode).IsRequired();
			entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
			entity.Property(x => x.IsHistoric).IsRequired();
			entity.Property(x => x.Priority).IsRequired();

			entity.HasIndex(x => x.CurrencyCode).IsUnique();
			entity.HasIndex(x => x.Priority).IsUnique();
		});

		modelBuilder.Entity<CentralBankEntity>(entity => {
			entity.ToTable("CentralBank");

			entity.HasKey(x => x.Id);

			entity.Property(x => x.BankCode).HasMaxLength(50).IsRequired();
			entity.Property(x => x.BankName).HasMaxLength(255).IsRequired();
			entity.Property(x => x.IsActive).IsRequired();
			entity.Property(x => x.CreatedAtUtc).IsRequired();
			entity.Property(x => x.CountryOfOrigin).HasMaxLength(100).IsRequired(false);
			entity.HasIndex(x => x.BankCode).IsUnique();
			entity.HasIndex(x => x.Priority).IsUnique().HasFilter("[Priority] IS NOT NULL");

			entity.HasOne(x => x.NativeCurrency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
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

			entity.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(x => x.PeggedOnCurrency).WithMany().HasForeignKey(x => x.PeggedOnCurrencyId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<CentralBankSupportedCurrencyEntity>(entity => {
			entity.ToTable("CentralBankSupportedCurrency");
			entity.HasKey(x => new { x.CentralBankId, x.CurrencyId });

			entity.Property(x => x.ProviderSeriesId).HasMaxLength(50).IsRequired(false);

			entity.HasOne(x => x.CentralBank).WithMany(x => x.SupportedCurrencies).HasForeignKey(x => x.CentralBankId);
			entity.HasOne(x => x.Currency).WithMany(x => x.SupportedByCentralBanks).HasForeignKey(x => x.CurrencyId);
		});

		modelBuilder.Entity<ExchangeRateUnavailableDateEntity>(entity => {
			entity.ToTable("ExchangeRateUnavailableDate");
			entity.HasKey(e => e.Id);

			entity.Property(e => e.BaseCurrency).HasMaxLength(3).IsFixedLength().IsRequired();
			entity.Property(e => e.QuoteCurrency).HasMaxLength(3).IsFixedLength().IsRequired();
			entity.Property(e => e.UnavailableDate).HasColumnType("date").IsRequired();
			entity.Property(e => e.CreatedAtUTC).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
			entity.Property(e => e.Reason).HasMaxLength(124).IsRequired(false);
			entity.HasIndex(e => new { e.CentralBankId, e.BaseCurrency, e.QuoteCurrency, e.UnavailableDate }).IsUnique().HasDatabaseName("UX_ExchangeRateUnavailableDate");
			entity.HasOne<CentralBankEntity>().WithMany().HasForeignKey(e => e.CentralBankId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<PreferredProviderEntity>(entity => {
			entity.ToTable("PreferredProvider");
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => new { x.CurrencyId, x.CentralBankId }).IsUnique();
			entity.HasIndex(x => new { x.CurrencyId, x.IsActive, x.Priority });

			entity.HasOne(x => x.Currency).WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(x => x.CentralBank).WithMany().HasForeignKey(x => x.CentralBankId).OnDelete(DeleteBehavior.Restrict);
		});
	}
}