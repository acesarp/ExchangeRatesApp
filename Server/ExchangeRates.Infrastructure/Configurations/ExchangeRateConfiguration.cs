using ExchangeRates.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExchangeRates.Infrastructure.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRateEntity> {
	public void Configure(EntityTypeBuilder<ExchangeRateEntity> builder) {
		builder.HasKey(x => x.Id);

		builder.HasIndex(x => new {
			x.Date,
			x.BaseCurrency,
			x.QuoteCurrency,
			x.Provider
		}).IsUnique();

		builder.Property(x => x.Rate)
			.HasPrecision(28, 12);

		builder.Property(x => x.Provider)
			.HasMaxLength(20)
			.IsRequired();
	}
}