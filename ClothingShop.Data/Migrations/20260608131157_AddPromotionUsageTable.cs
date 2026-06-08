using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionUsageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Policy",
                table: "Products",
                newName: "policy");

            migrationBuilder.AddColumn<int>(
                name: "TotalUsageLimit",
                table: "Promotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsedCount",
                table: "Promotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PromotionUsages",
                columns: table => new
                {
                    UsageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromotionId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionUsages", x => x.UsageId);
                    table.ForeignKey(
                        name: "FK_PromotionUsages_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "promotion_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_PromotionId",
                table: "PromotionUsages",
                column: "PromotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "TotalUsageLimit",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "UsedCount",
                table: "Promotions");

            migrationBuilder.RenameColumn(
                name: "policy",
                table: "Products",
                newName: "Policy");
        }
    }
}
