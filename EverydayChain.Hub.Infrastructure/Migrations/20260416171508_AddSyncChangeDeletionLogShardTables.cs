using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncChangeDeletionLogShardTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sync_change_logs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ParentBatchId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TableCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    BusinessKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BeforeSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_change_logs", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateTable(
                name: "sync_deletion_logs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ParentBatchId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TableCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BusinessKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DeletionPolicy = table.Column<int>(type: "int", nullable: false),
                    Executed = table.Column<bool>(type: "bit", nullable: false),
                    DeletedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceEvidence = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CreatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_deletion_logs", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sync_change_logs_BatchId",
                schema: "dbo",
                table: "sync_change_logs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_sync_change_logs_TableCode_ChangedTimeLocal",
                schema: "dbo",
                table: "sync_change_logs",
                columns: new[] { "TableCode", "ChangedTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_sync_deletion_logs_BatchId",
                schema: "dbo",
                table: "sync_deletion_logs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_sync_deletion_logs_TableCode_DeletedTimeLocal",
                schema: "dbo",
                table: "sync_deletion_logs",
                columns: new[] { "TableCode", "DeletedTimeLocal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_change_logs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "sync_deletion_logs",
                schema: "dbo");
        }
    }
}
