using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QR_Menu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLicenseTypeIdFromLicensePackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LicenseTypeId",
                table: "LicensePackages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LicenseTypeId",
                table: "LicensePackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
