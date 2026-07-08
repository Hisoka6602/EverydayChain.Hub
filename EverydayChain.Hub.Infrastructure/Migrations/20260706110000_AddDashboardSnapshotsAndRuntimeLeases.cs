using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <summary>
    /// 新增看板快照表与运行时租约表。
    /// </summary>
    public partial class AddDashboardSnapshotsAndRuntimeLeases : Migration
    {
        /// <summary>
        /// 执行迁移升级。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dashboard_scan_snapshots",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BucketStartLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalScanCount = table.Column<int>(type: "int", nullable: false),
                    MatchedScanCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_scan_snapshots", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_snapshot_states",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    CoverageStartLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CoverageEndLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRefreshTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_snapshot_states", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_task_snapshots",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WaveCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WaveRemark = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ResolvedDockCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkingArea = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    BucketStartLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    ScannedCount = table.Column<int>(type: "int", nullable: false),
                    RecirculatedCount = table.Column<int>(type: "int", nullable: false),
                    ExceptionCount = table.Column<int>(type: "int", nullable: false),
                    RequiredFeedbackCount = table.Column<int>(type: "int", nullable: false),
                    CompletedFeedbackCount = table.Column<int>(type: "int", nullable: false),
                    TotalVolumeMm3 = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    TotalWeightGram = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    EarliestCreatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LatestUpdatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_task_snapshots", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateTable(
                name: "runtime_leases",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AcquiredTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtLocal = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runtime_leases", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_scan_snapshots_BucketStartLocal",
                schema: "dbo",
                table: "dashboard_scan_snapshots",
                column: "BucketStartLocal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_task_snapshots_BucketStartLocal",
                schema: "dbo",
                table: "dashboard_task_snapshots",
                column: "BucketStartLocal");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_task_snapshots_BucketStartLocal_ResolvedDockCode",
                schema: "dbo",
                table: "dashboard_task_snapshots",
                columns: new[] { "BucketStartLocal", "ResolvedDockCode" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_task_snapshots_BucketStartLocal_WaveCode",
                schema: "dbo",
                table: "dashboard_task_snapshots",
                columns: new[] { "BucketStartLocal", "WaveCode" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_task_snapshots_BucketStartLocal_WaveCode_ResolvedDockCode",
                schema: "dbo",
                table: "dashboard_task_snapshots",
                columns: new[] { "BucketStartLocal", "WaveCode", "ResolvedDockCode" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_task_snapshots_BucketStartLocal_WaveCode_SourceType_Status",
                schema: "dbo",
                table: "dashboard_task_snapshots",
                columns: new[] { "BucketStartLocal", "WaveCode", "SourceType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_task_snapshots_BucketStartLocal_WaveCode_WorkingArea_SourceType_Status",
                schema: "dbo",
                table: "dashboard_task_snapshots",
                columns: new[] { "BucketStartLocal", "WaveCode", "WorkingArea", "SourceType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_runtime_leases_ExpiresAtLocal",
                schema: "dbo",
                table: "runtime_leases",
                column: "ExpiresAtLocal");
        }

        /// <summary>
        /// 执行迁移回滚。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dashboard_scan_snapshots",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "dashboard_snapshot_states",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "dashboard_task_snapshots",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "runtime_leases",
                schema: "dbo");
        }
    }
}
