using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QR_Menu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyOrderTagModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderTag_RestaurantId_TagType_IsActive_DisplayOrder",
                table: "OrderTags");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "OrderTags");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "OrderTags");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "OrderTags");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "OrderTags");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OrderTags");

            migrationBuilder.DropColumn(
                name: "TagType",
                table: "OrderTags");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "OrderTags",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderTag_RestaurantId",
                table: "OrderTags",
                column: "RestaurantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderTag_RestaurantId",
                table: "OrderTags");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "OrderTags",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "OrderTags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "OrderTags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "OrderTags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "OrderTags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OrderTags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TagType",
                table: "OrderTags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OrderTag_RestaurantId_TagType_IsActive_DisplayOrder",
                table: "OrderTags",
                columns: new[] { "RestaurantId", "TagType", "IsActive", "DisplayOrder" });
        }
    }
}
