using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeRates.Infrastructure.Migrations;

/// <inheritdoc />
public partial class RemoveProviderColumnFix : Migration {
	/// <inheritdoc />
	protected override void Up(MigrationBuilder migrationBuilder) {
		migrationBuilder.DropIndex(name: "IX_ExchangeRates_Date_BaseCurrency_QuoteCurrency_Provider", table: "ExchangeRates");

		migrationBuilder.DropColumn(name: "Provider", table: "ExchangeRates");
		migrationBuilder.CreateIndex(name: "IX_ExchangeRates_Date_BaseCurrency_QuoteCurrency", table: "ExchangeRates", columns: new[] { "Date", "BaseCurrency", "QuoteCurrency" }, unique: true);

	}

	/// <inheritdoc />
	protected override void Down(MigrationBuilder migrationBuilder) {

	}
}
