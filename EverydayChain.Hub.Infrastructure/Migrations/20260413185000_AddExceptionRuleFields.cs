using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionRuleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WaveCode",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecirculated",
                schema: "dbo",
                table: "business_tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ScanRetryCount",
                schema: "dbo",
                table: "business_tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_WaveCode",
                schema: "dbo",
                table: "business_tasks",
                column: "WaveCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_WaveCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "WaveCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "IsRecirculated",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "ScanRetryCount",
                schema: "dbo",
                table: "business_tasks");
        }
    }
}
