using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHasReviewedToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasReviewed",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasReviewed",
                table: "Orders");
        }
    }
}
