using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <summary>
    /// 为高并发读路径补充热点索引。
    /// </summary>
    public partial class OptimizeHotPathReadIndexes : Migration
    {
        /// <summary>
        /// 执行迁移升级。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 步骤：为业务任务的 chute 查询与当前波次回退查询建立复合索引。
            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_TargetChuteCode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "TargetChuteCode", "CreatedTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_ActualChuteCode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "ActualChuteCode", "CreatedTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_ScannedAtLocal_Id",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "ScannedAtLocal", "Id" });

            // 步骤：为扫描日志的时间分页、扫码枪筛选与按时间倒序翻页建立复合索引。
            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_DeviceCode",
                schema: "dbo",
                table: "scan_logs",
                column: "DeviceCode");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_DeviceCode_ScanTimeLocal",
                schema: "dbo",
                table: "scan_logs",
                columns: new[] { "DeviceCode", "ScanTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_ScanTimeLocal_Id",
                schema: "dbo",
                table: "scan_logs",
                columns: new[] { "ScanTimeLocal", "Id" });
        }

        /// <summary>
        /// 执行迁移回滚。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 步骤：删除本次为热点读路径新增的索引。
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_TargetChuteCode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_ActualChuteCode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_CreatedTimeLocal_ScannedAtLocal_Id",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_scan_logs_DeviceCode",
                schema: "dbo",
                table: "scan_logs");

            migrationBuilder.DropIndex(
                name: "IX_scan_logs_DeviceCode_ScanTimeLocal",
                schema: "dbo",
                table: "scan_logs");

            migrationBuilder.DropIndex(
                name: "IX_scan_logs_ScanTimeLocal_Id",
                schema: "dbo",
                table: "scan_logs");
        }
    }
}
