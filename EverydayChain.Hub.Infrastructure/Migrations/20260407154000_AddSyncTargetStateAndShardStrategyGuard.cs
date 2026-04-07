using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations;

/// <summary>
/// 新增同步目标状态表，并作为分表策略纠偏迁移基线。
/// </summary>
[Migration("20260407154000_AddSyncTargetStateAndShardStrategyGuard")]
public class AddSyncTargetStateAndShardStrategyGuard : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "dbo");

        migrationBuilder.CreateTable(
            name: "sync_target_state",
            schema: "dbo",
            columns: table => new
            {
                TableCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                BusinessKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                RowDigest = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                CursorLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsSoftDeleted = table.Column<bool>(type: "bit", nullable: false),
                SoftDeletedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                ShardSuffix = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                TargetLogicalTable = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                UpdatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sync_target_state", x => new { x.TableCode, x.BusinessKey });
            });

        migrationBuilder.CreateIndex(
            name: "IX_sync_target_state_TableCode_CursorLocal",
            schema: "dbo",
            table: "sync_target_state",
            columns: new[] { "TableCode", "CursorLocal" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "sync_target_state",
            schema: "dbo");
    }
}
