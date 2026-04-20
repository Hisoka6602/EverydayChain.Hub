using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _20260420221000_AdjustBusinessTaskRecirculationAndHotPathIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_IsRecirculated",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_CreatedTimeLocal_SourceType_Status_IsException_IsRecirculated",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_NormalizedBarcode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "NormalizedBarcode", "CreatedTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_SourceType_Status_IsException_ResolvedDockCode",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "SourceType", "Status", "IsException", "ResolvedDockCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_NormalizedBarcode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_CreatedTimeLocal_SourceType_Status_IsException_ResolvedDockCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_IsRecirculated",
                schema: "dbo",
                table: "business_tasks",
                column: "IsRecirculated");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_SourceType_Status_IsException_IsRecirculated",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "SourceType", "Status", "IsException", "IsRecirculated" });
        }
    }
}
