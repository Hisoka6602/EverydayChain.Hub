using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTaskQueryOptimizationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_CreatedTimeLocal_WaveCode_TargetChuteCode_ActualChuteCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedBarcode",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedWaveCode",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedDockCode",
                schema: "dbo",
                table: "business_tasks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "未分配码头");

            migrationBuilder.Sql(
                """
                UPDATE [dbo].[business_tasks]
                SET [NormalizedBarcode] = CASE
                                              WHEN [Barcode] IS NULL OR LTRIM(RTRIM([Barcode])) = N'' THEN NULL
                                              ELSE LTRIM(RTRIM([Barcode]))
                                          END,
                    [NormalizedWaveCode] = CASE
                                               WHEN [WaveCode] IS NULL OR LTRIM(RTRIM([WaveCode])) = N'' THEN NULL
                                               ELSE LTRIM(RTRIM([WaveCode]))
                                           END,
                    [ResolvedDockCode] = CASE
                                             WHEN [ActualChuteCode] IS NOT NULL AND LTRIM(RTRIM([ActualChuteCode])) <> N'' THEN LTRIM(RTRIM([ActualChuteCode]))
                                             WHEN [TargetChuteCode] IS NOT NULL AND LTRIM(RTRIM([TargetChuteCode])) <> N'' THEN LTRIM(RTRIM([TargetChuteCode]))
                                             ELSE N'未分配码头'
                                         END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_NormalizedWaveCode_ResolvedDockCode",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "NormalizedWaveCode", "ResolvedDockCode" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_NormalizedBarcode",
                schema: "dbo",
                table: "business_tasks",
                column: "NormalizedBarcode");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_NormalizedWaveCode",
                schema: "dbo",
                table: "business_tasks",
                column: "NormalizedWaveCode");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_NormalizedWaveCode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "NormalizedWaveCode", "CreatedTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_ResolvedDockCode",
                schema: "dbo",
                table: "business_tasks",
                column: "ResolvedDockCode");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_ResolvedDockCode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "ResolvedDockCode", "CreatedTimeLocal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_CreatedTimeLocal_NormalizedWaveCode_ResolvedDockCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_NormalizedBarcode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_NormalizedWaveCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_NormalizedWaveCode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_ResolvedDockCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropIndex(
                name: "IX_business_tasks_ResolvedDockCode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "NormalizedBarcode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "NormalizedWaveCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.DropColumn(
                name: "ResolvedDockCode",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_WaveCode_TargetChuteCode_ActualChuteCode",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "WaveCode", "TargetChuteCode", "ActualChuteCode" });
        }
    }
}
