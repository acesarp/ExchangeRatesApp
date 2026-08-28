
using ExchangeRates.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExchangeRates.Infrastructure.Configurations;

public sealed class ExchangeRateFetchConfiguration : IEntityTypeConfiguration<ExchangeRateFetch> {
	public void Configure(EntityTypeBuilder<ExchangeRateFetch> builder) {
		builder.HasKey(x => x.Id);

		builder.HasIndex(x => new {
			x.Provider,
			x.BaseCurrency,
			x.QuoteCurrency,
			x.FromDate,
			x.ToDate
		}).IsUnique();

		builder.Property(x => x.Provider).HasMaxLength(20).IsRequired();
	}
}