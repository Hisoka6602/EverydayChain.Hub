using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RebuildInitialHubSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "IDX_PICKTOLIGHT_CARTON1",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DOCNO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WORKINGAREA = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CARTONNO = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SORTATIONLOCATION = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    USEFLAG = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    ADDITIONAL = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    OPENTIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CLOSETIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SEQNO = table.Column<int>(type: "int", nullable: true),
                    ADDTIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ADDWHO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TASKPROCESS = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    WAVENO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    STOREID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    STOP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    WAREHOUSEID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MENDIAN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WCSNO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LENGTH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    WIDTH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    HIGH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    CUBE = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    GROSSWEIGHT = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    CONSIGNEEID = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DESCR = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SCANCOUNT = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "IDX_PICKTOWCS2",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WAVENO = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DOCNO = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    QTY = table.Column<int>(type: "int", nullable: true),
                    SEQNO = table.Column<int>(type: "int", nullable: true),
                    FLAG = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    ADDTIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EDITTIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MENDIAN = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    WCSNO = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SKUID = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SKUSEQ = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SKUQTY = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SKU = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LOCATION = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ZJFLAG = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    SKUQTY1 = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    ALLNUM = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ALLNUM1 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    R_SYSID = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LENGTH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    WIDTH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    HIGH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    CUBE = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    GROSSWEIGHT = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    CONSIGNEEID = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DESCR = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SCANCOUNT = table.Column<int>(type: "int", nullable: true),
                    OPENTIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CLOSETIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TASKPROCESS = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "sorting_task_trace",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "IX_IDX_PICKTOLIGHT_CARTON1_ADDTIME",
                schema: "dbo",
                table: "IDX_PICKTOLIGHT_CARTON1",
                column: "ADDTIME");

            migrationBuilder.CreateIndex(
                name: "IX_IDX_PICKTOLIGHT_CARTON1_CARTONNO",
                schema: "dbo",
                table: "IDX_PICKTOLIGHT_CARTON1",
                column: "CARTONNO",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IDX_PICKTOWCS2_ADDTIME",
                schema: "dbo",
                table: "IDX_PICKTOWCS2",
                column: "ADDTIME");

            migrationBuilder.CreateIndex(
                name: "IX_IDX_PICKTOWCS2_R_SYSID",
                schema: "dbo",
                table: "IDX_PICKTOWCS2",
                column: "R_SYSID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sorting_task_trace_BusinessNo",
                schema: "dbo",
                table: "sorting_task_trace",
                column: "BusinessNo");

            migrationBuilder.CreateIndex(
                name: "IX_sorting_task_trace_CreatedAt",
                schema: "dbo",
                table: "sorting_task_trace",
                column: "CreatedAt");

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[IDX_PICKTOLIGHT_CARTON1]', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE [name] = N'PK_IDX_PICKTOLIGHT_CARTON1' AND [parent_object_id] = OBJECT_ID(N'[dbo].[IDX_PICKTOLIGHT_CARTON1]'))
                BEGIN
                    ALTER TABLE [dbo].[IDX_PICKTOLIGHT_CARTON1] ADD CONSTRAINT [PK_IDX_PICKTOLIGHT_CARTON1] PRIMARY KEY CLUSTERED ([Id] DESC);
                END;

                IF OBJECT_ID(N'[dbo].[IDX_PICKTOWCS2]', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE [name] = N'PK_IDX_PICKTOWCS2' AND [parent_object_id] = OBJECT_ID(N'[dbo].[IDX_PICKTOWCS2]'))
                BEGIN
                    ALTER TABLE [dbo].[IDX_PICKTOWCS2] ADD CONSTRAINT [PK_IDX_PICKTOWCS2] PRIMARY KEY CLUSTERED ([Id] DESC);
                END;

                IF OBJECT_ID(N'[dbo].[sorting_task_trace]', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE [name] = N'PK_sorting_task_trace' AND [parent_object_id] = OBJECT_ID(N'[dbo].[sorting_task_trace]'))
                BEGIN
                    ALTER TABLE [dbo].[sorting_task_trace] ADD CONSTRAINT [PK_sorting_task_trace] PRIMARY KEY CLUSTERED ([Id] DESC);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IDX_PICKTOLIGHT_CARTON1",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IDX_PICKTOWCS2",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "sorting_task_trace",
                schema: "dbo");
        }
    }
}
