using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QR_Menu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantSocialLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SocialLinks",
                table: "Restaurants",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SocialLinks",
                table: "Restaurants");
        }
    }
}
