using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QR_Menu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNameAndTimeIdToLicensePackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "LicensePackages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TimeId",
                table: "LicensePackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "LicensePackages");

            migrationBuilder.DropColumn(
                name: "TimeId",
                table: "LicensePackages");
        }
    }
}
