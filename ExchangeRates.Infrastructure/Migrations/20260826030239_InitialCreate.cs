using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeRates.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    BaseCurrency = table.Column<int>(type: "int", nullable: false),
                    QuoteCurrency = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_Date_BaseCurrency_QuoteCurrency_Provider",
                table: "ExchangeRates",
                columns: new[] { "Date", "BaseCurrency", "QuoteCurrency", "Provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeRates");
        }
    }
}
