using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <summary>
    /// 定义 _20260420221000_AdjustBusinessTaskRecirculationAndHotPathIndexes 类型。
    /// </summary>
    public partial class _20260420221000_AdjustBusinessTaskRecirculationAndHotPathIndexes : Migration
    {
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

