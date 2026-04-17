using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTaskClosureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FeedbackTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightMm",
                schema: "dbo",
                table: "business_tasks",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsException",
                schema: "dbo",
                table: "business_tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeedbackReported",
                schema: "dbo",
                table: "business_tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthMm",
                schema: "dbo",
                table: "business_tasks",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScanCount",
                schema: "dbo",
                table: "business_tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                schema: "dbo",
                table: "business_tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumeMm3",
                schema: "dbo",
                table: "business_tasks",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaveRemark",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightGram",
                schema: "dbo",
                table: "business_tasks",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthMm",
                schema: "dbo",
                table: "business_tasks",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_IsException",
                schema: "dbo",
                table: "business_tasks",
                column: "IsException");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_SourceType",
                schema: "dbo",
                table: "business_tasks",
                column: "SourceType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_IsException",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_SourceType",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "FeedbackTimeLocal",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "HeightMm",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "IsException",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "IsFeedbackReported",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "LengthMm",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "ScanCount",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "SourceType",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "VolumeMm3",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "WaveRemark",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "WeightGram",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "WidthMm",
                schema: "dbo",
                table: "business_tasks");
        }
    }
}
