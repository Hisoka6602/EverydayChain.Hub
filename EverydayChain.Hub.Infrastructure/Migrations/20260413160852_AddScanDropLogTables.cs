using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScanDropLogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "drop_logs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessTaskId = table.Column<long>(type: "bigint", nullable: true),
                    TaskCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ActualChuteCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DropTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_drop_logs", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateTable(
                name: "scan_logs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessTaskId = table.Column<long>(type: "bigint", nullable: true),
                    TaskCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DeviceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsMatched = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ScanTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_logs", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateIndex(
                name: "IX_drop_logs_BusinessTaskId",
                schema: "dbo",
                table: "drop_logs",
                column: "BusinessTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_drop_logs_DropTimeLocal",
                schema: "dbo",
                table: "drop_logs",
                column: "DropTimeLocal");

            migrationBuilder.CreateIndex(
                name: "IX_drop_logs_TaskCode",
                schema: "dbo",
                table: "drop_logs",
                column: "TaskCode");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_Barcode",
                schema: "dbo",
                table: "scan_logs",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_BusinessTaskId",
                schema: "dbo",
                table: "scan_logs",
                column: "BusinessTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_ScanTimeLocal",
                schema: "dbo",
                table: "scan_logs",
                column: "ScanTimeLocal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "drop_logs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "scan_logs",
                schema: "dbo");
        }
    }
}
