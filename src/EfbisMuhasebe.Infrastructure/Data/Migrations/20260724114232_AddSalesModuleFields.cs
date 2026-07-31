using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EfbisMuhasebe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesModuleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableForSale",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SaleStatusUpdatedBy",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleStatusUpdatedDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsAvailableForSale",
                table: "Products",
                column: "IsAvailableForSale");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SaleStatus_Status",
                table: "Products",
                columns: new[] { "IsAvailableForSale", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_IsAvailableForSale",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SaleStatus_Status",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsAvailableForSale",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SaleStatusUpdatedBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SaleStatusUpdatedDate",
                table: "Products");
        }
    }
}
