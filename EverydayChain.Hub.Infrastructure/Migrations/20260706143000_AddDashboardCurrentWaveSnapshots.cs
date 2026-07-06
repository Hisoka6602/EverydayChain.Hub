using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <summary>
    /// 为当前波次自动识别增加分钟快照表。
    /// </summary>
    public partial class AddDashboardCurrentWaveSnapshots : Migration
    {
        /// <summary>
        /// 执行迁移升级。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 步骤：创建当前波次分钟快照表，供高并发看板查询直接读取。
            migrationBuilder.CreateTable(
                name: "dashboard_current_wave_snapshots",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BucketStartLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScannedAtLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WaveCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WaveRemark = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_current_wave_snapshots", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            // 步骤：建立分钟桶唯一索引，确保每分钟只保留一条当前波次快照。
            migrationBuilder.CreateIndex(
                name: "IX_dashboard_current_wave_snapshots_BucketStartLocal",
                schema: "dbo",
                table: "dashboard_current_wave_snapshots",
                column: "BucketStartLocal",
                unique: true);

            // 步骤：建立扫描时间复合索引，提升按时间段获取最新波次的查询性能。
            migrationBuilder.CreateIndex(
                name: "IX_dashboard_current_wave_snapshots_BucketStartLocal_ScannedAtLocal",
                schema: "dbo",
                table: "dashboard_current_wave_snapshots",
                columns: new[] { "BucketStartLocal", "ScannedAtLocal" });
        }

        /// <summary>
        /// 执行迁移回滚。
        /// </summary>
        /// <param name="migrationBuilder">迁移构建器。</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 步骤：删除当前波次分钟快照表及其索引。
            migrationBuilder.DropTable(
                name: "dashboard_current_wave_snapshots",
                schema: "dbo");
        }
    }
}
