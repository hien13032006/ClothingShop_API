using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductAndCartStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Policy",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Policy",
                table: "Products");
        }
    }
}
