using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QR_Menu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLicensePackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LicensePackageId",
                table: "Licenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LicensePackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseTypeId = table.Column<int>(type: "integer", nullable: false),
                    Time = table.Column<int>(type: "integer", nullable: false),
                    UserPrice = table.Column<double>(type: "double precision", nullable: false),
                    DealerPrice = table.Column<double>(type: "double precision", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdateDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicensePackages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_LicensePackageId",
                table: "Licenses",
                column: "LicensePackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Licenses_LicensePackages_LicensePackageId",
                table: "Licenses",
                column: "LicensePackageId",
                principalTable: "LicensePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Licenses_LicensePackages_LicensePackageId",
                table: "Licenses");

            migrationBuilder.DropTable(
                name: "LicensePackages");

            migrationBuilder.DropIndex(
                name: "IX_Licenses_LicensePackageId",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "LicensePackageId",
                table: "Licenses");
        }
    }
}
