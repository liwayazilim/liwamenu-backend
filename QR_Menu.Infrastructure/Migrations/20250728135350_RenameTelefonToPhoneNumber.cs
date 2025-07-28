using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QR_Menu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTelefonToPhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Telefon",
                table: "Restaurants",
                newName: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Restaurants",
                newName: "Telefon");
        }
    }
}
