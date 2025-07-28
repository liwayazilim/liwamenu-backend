using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QR_Menu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantImageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "Restaurants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Restaurants",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "Restaurants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "Restaurants");
        }
    }
}
