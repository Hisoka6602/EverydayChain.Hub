using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeWmsPickToWcsUniqueIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IDX_PICKTOWCS2_R_SYSID",
                schema: "dbo",
                table: "IDX_PICKTOWCS2");

            migrationBuilder.AlterColumn<string>(
                name: "R_SYSID",
                schema: "dbo",
                table: "IDX_PICKTOWCS2",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateIndex(
                name: "IX_IDX_PICKTOWCS2_R_SYSID",
                schema: "dbo",
                table: "IDX_PICKTOWCS2",
                column: "R_SYSID",
                unique: true,
                filter: "[R_SYSID] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IDX_PICKTOWCS2_R_SYSID",
                schema: "dbo",
                table: "IDX_PICKTOWCS2");

            migrationBuilder.AlterColumn<string>(
                name: "R_SYSID",
                schema: "dbo",
                table: "IDX_PICKTOWCS2",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IDX_PICKTOWCS2_R_SYSID",
                schema: "dbo",
                table: "IDX_PICKTOWCS2",
                column: "R_SYSID",
                unique: true);
        }
    }
}
