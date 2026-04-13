using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTaskTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "business_tasks",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceTableCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BusinessKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TargetChuteCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ActualChuteCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DeviceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FeedbackStatus = table.Column<int>(type: "int", nullable: false),
                    ScannedAtLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DroppedAtLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_tasks", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_Barcode",
                schema: "dbo",
                table: "business_tasks",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                column: "CreatedTimeLocal");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_Status",
                schema: "dbo",
                table: "business_tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_TaskCode",
                schema: "dbo",
                table: "business_tasks",
                column: "TaskCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "business_tasks",
                schema: "dbo");
        }
    }
}
