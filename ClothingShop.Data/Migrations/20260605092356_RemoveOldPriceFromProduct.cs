using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldPriceFromProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "old_price",
                table: "Products");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Promotions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountAmount",
                table: "Promotions",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                table: "Promotions");

            migrationBuilder.AddColumn<decimal>(
                name: "old_price",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
