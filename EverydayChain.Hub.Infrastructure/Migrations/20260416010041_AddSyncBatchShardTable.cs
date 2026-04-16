using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncBatchShardTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sync_batches",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ParentBatchId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TableCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WindowStartLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WindowEndLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadCount = table.Column<int>(type: "int", nullable: false),
                    InsertCount = table.Column<int>(type: "int", nullable: false),
                    UpdateCount = table.Column<int>(type: "int", nullable: false),
                    DeleteCount = table.Column<int>(type: "int", nullable: false),
                    SkipCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_batches", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sync_batches_BatchId",
                schema: "dbo",
                table: "sync_batches",
                column: "BatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sync_batches_TableCode_Status_CompletedTimeLocal",
                schema: "dbo",
                table: "sync_batches",
                columns: new[] { "TableCode", "Status", "CompletedTimeLocal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_batches",
                schema: "dbo");
        }
    }
}
