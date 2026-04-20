using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _20260420213000_StrengthenHotPathIndexesAndRecirculationFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_CreatedTimeLocal",
                schema: "dbo",
                table: "scan_logs",
                column: "CreatedTimeLocal");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_CreatedTimeLocal_ScanTimeLocal",
                schema: "dbo",
                table: "scan_logs",
                columns: new[] { "CreatedTimeLocal", "ScanTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_CreatedTimeLocal_TaskCode",
                schema: "dbo",
                table: "scan_logs",
                columns: new[] { "CreatedTimeLocal", "TaskCode" });

            migrationBuilder.CreateIndex(
                name: "IX_drop_logs_CreatedTimeLocal",
                schema: "dbo",
                table: "drop_logs",
                column: "CreatedTimeLocal");

            migrationBuilder.CreateIndex(
                name: "IX_drop_logs_CreatedTimeLocal_DropTimeLocal",
                schema: "dbo",
                table: "drop_logs",
                columns: new[] { "CreatedTimeLocal", "DropTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_drop_logs_CreatedTimeLocal_TaskCode",
                schema: "dbo",
                table: "drop_logs",
                columns: new[] { "CreatedTimeLocal", "TaskCode" });

            migrationBuilder.CreateIndex(
                name: "IX_drop_logs_IsSuccess",
                schema: "dbo",
                table: "drop_logs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_NormalizedWaveCode",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "NormalizedWaveCode" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_ResolvedDockCode",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "ResolvedDockCode" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_Status_SourceType",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "Status", "SourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_DroppedAtLocal",
                schema: "dbo",
                table: "business_tasks",
                column: "DroppedAtLocal");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_NormalizedWaveCode_SourceType_WorkingArea",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "NormalizedWaveCode", "SourceType", "WorkingArea" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_NormalizedWaveCode_UpdatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "NormalizedWaveCode", "UpdatedTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_ScannedAtLocal",
                schema: "dbo",
                table: "business_tasks",
                column: "ScannedAtLocal");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_WorkingArea",
                schema: "dbo",
                table: "business_tasks",
                column: "WorkingArea");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_scan_logs_CreatedTimeLocal",
                schema: "dbo",
                table: "scan_logs");

            migrationBuilder.DropIndex(
                name: "IX_scan_logs_CreatedTimeLocal_ScanTimeLocal",
                schema: "dbo",
                table: "scan_logs");

            migrationBuilder.DropIndex(
                name: "IX_scan_logs_CreatedTimeLocal_TaskCode",
                schema: "dbo",
                table: "scan_logs");

            migrationBuilder.DropIndex(
                name: "IX_drop_logs_CreatedTimeLocal",
                schema: "dbo",
                table: "drop_logs");

            migrationBuilder.DropIndex(
                name: "IX_drop_logs_CreatedTimeLocal_DropTimeLocal",
                schema: "dbo",
                table: "drop_logs");

            migrationBuilder.DropIndex(
                name: "IX_drop_logs_CreatedTimeLocal_TaskCode",
                schema: "dbo",
                table: "drop_logs");

            migrationBuilder.DropIndex(
                name: "IX_drop_logs_IsSuccess",
                schema: "dbo",
                table: "drop_logs");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_CreatedTimeLocal_NormalizedWaveCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_CreatedTimeLocal_ResolvedDockCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_CreatedTimeLocal_Status_SourceType",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_DroppedAtLocal",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_NormalizedWaveCode_SourceType_WorkingArea",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_NormalizedWaveCode_UpdatedTimeLocal",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_ScannedAtLocal",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_WorkingArea",
                schema: "dbo",
                table: "business_tasks");
        }
    }
}
