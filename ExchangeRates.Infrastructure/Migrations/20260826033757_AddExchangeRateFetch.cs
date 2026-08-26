using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExchangeRates.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeRateFetch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExchangeRateFetches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BaseCurrency = table.Column<int>(type: "int", nullable: false),
                    QuoteCurrency = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FetchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateFetches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateFetches_Provider_BaseCurrency_QuoteCurrency_FromDate_ToDate",
                table: "ExchangeRateFetches",
                columns: new[] { "Provider", "BaseCurrency", "QuoteCurrency", "FromDate", "ToDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeRateFetches");
        }
    }
}
