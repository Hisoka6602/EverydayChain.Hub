using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLocalMirrorTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IDX_PICKTOLIGHT_CARTON1",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IDX_PICKTOWCS2",
                schema: "dbo");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_SourceTableCode_BusinessKey",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "SourceTableCode", "BusinessKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_business_tasks_SourceTableCode_BusinessKey",
                schema: "dbo",
                table: "business_tasks");

            migrationBuilder.CreateTable(
                name: "IDX_PICKTOLIGHT_CARTON1",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ADDTIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ADDWHO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ADDITIONAL = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    CARTONNO = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SEQNO = table.Column<int>(type: "int", nullable: true),
                    CLOSETIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CONSIGNEEID = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DESCR = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DOCNO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GROSSWEIGHT = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    HIGH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    LENGTH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    OPENTIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SCANCOUNT = table.Column<int>(type: "int", nullable: true),
                    SORTATIONLOCATION = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    STOP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    STOREID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MENDIAN = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TASKPROCESS = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    USEFLAG = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    CUBE = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    WAREHOUSEID = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WAVENO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WCSNO = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    WIDTH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    WORKINGAREA = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IDX_PICKTOLIGHT_CARTON1", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

            migrationBuilder.CreateTable(
                name: "IDX_PICKTOWCS2",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ADDTIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SKUID = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SEQNO = table.Column<int>(type: "int", nullable: true),
                    CLOSETIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CONSIGNEEID = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DESCR = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DOCNO = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EDITTIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FLAG = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    GROSSWEIGHT = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    HIGH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    LENGTH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    LOCATION = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SKUQTY1 = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    OPENTIME = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QTY = table.Column<int>(type: "int", nullable: true),
                    SCANCOUNT = table.Column<int>(type: "int", nullable: true),
                    ALLNUM = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ALLNUM1 = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SKU = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SKUQTY = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SKUSEQ = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    STATUS = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    MENDIAN = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TASKPROCESS = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    R_SYSID = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CUBE = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    WAVENO = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    WCSNO = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    WIDTH = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    ZJFLAG = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IDX_PICKTOWCS2", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
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
                name: "IX_IDX_PICKTOWCS2_DOCNO_ADDTIME",
                schema: "dbo",
                table: "IDX_PICKTOWCS2",
                columns: new[] { "DOCNO", "ADDTIME" });

            migrationBuilder.CreateIndex(
                name: "IX_IDX_PICKTOWCS2_R_SYSID",
                schema: "dbo",
                table: "IDX_PICKTOWCS2",
                column: "R_SYSID");
        }
    }
}
