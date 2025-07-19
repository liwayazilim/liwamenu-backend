using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QR_Menu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixNullSecurityStamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix existing users with null SecurityStamp
            migrationBuilder.Sql(@"
                UPDATE ""AspNetUsers"" 
                SET ""SecurityStamp"" = gen_random_uuid()::text 
                WHERE ""SecurityStamp"" IS NULL OR ""SecurityStamp"" = '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed for this data fix
        }
    }
}
