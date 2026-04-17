using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EverydayChain.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RebuildHubBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "business_tasks",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceTableCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
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
                    LengthMm = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    WidthMm = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    HeightMm = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    VolumeMm3 = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    WeightGram = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    ScanCount = table.Column<int>(type: "int", nullable: false),
                    DroppedAtLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WaveCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    WaveRemark = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsRecirculated = table.Column<bool>(type: "bit", nullable: false),
                    IsException = table.Column<bool>(type: "bit", nullable: false),
                    ScanRetryCount = table.Column<int>(type: "int", nullable: false),
                    IsFeedbackReported = table.Column<bool>(type: "bit", nullable: false),
                    FeedbackTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_tasks", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

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
                    R_SYSID = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
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
                    table.PrimaryKey("PK_IDX_PICKTOWCS2", x => x.Id)
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
                    table.PrimaryKey("PK_sorting_task_trace", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                });

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
                name: "IX_business_tasks_ActualChuteCode",
                schema: "dbo",
                table: "business_tasks",
                column: "ActualChuteCode");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_Barcode",
                schema: "dbo",
                table: "business_tasks",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_Barcode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "Barcode", "CreatedTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                column: "CreatedTimeLocal");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_Id",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_SourceType_Status_IsException_IsRecirculated",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "SourceType", "Status", "IsException", "IsRecirculated" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_CreatedTimeLocal_WaveCode_TargetChuteCode_ActualChuteCode",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "CreatedTimeLocal", "WaveCode", "TargetChuteCode", "ActualChuteCode" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_FeedbackStatus",
                schema: "dbo",
                table: "business_tasks",
                column: "FeedbackStatus");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_FeedbackStatus_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "FeedbackStatus", "CreatedTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_FeedbackStatus_IsFeedbackReported_FeedbackTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "FeedbackStatus", "IsFeedbackReported", "FeedbackTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_FeedbackTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                column: "FeedbackTimeLocal");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_IsException",
                schema: "dbo",
                table: "business_tasks",
                column: "IsException");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_IsFeedbackReported",
                schema: "dbo",
                table: "business_tasks",
                column: "IsFeedbackReported");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_IsRecirculated",
                schema: "dbo",
                table: "business_tasks",
                column: "IsRecirculated");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_SourceType",
                schema: "dbo",
                table: "business_tasks",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_Status",
                schema: "dbo",
                table: "business_tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_TargetChuteCode",
                schema: "dbo",
                table: "business_tasks",
                column: "TargetChuteCode");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_TaskCode",
                schema: "dbo",
                table: "business_tasks",
                column: "TaskCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_WaveCode",
                schema: "dbo",
                table: "business_tasks",
                column: "WaveCode");

            migrationBuilder.CreateIndex(
                name: "IX_business_tasks_WaveCode_CreatedTimeLocal",
                schema: "dbo",
                table: "business_tasks",
                columns: new[] { "WaveCode", "CreatedTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_drop_logs_ActualChuteCode",
                schema: "dbo",
                table: "drop_logs",
                column: "ActualChuteCode");

            migrationBuilder.CreateIndex(
                name: "IX_drop_logs_Barcode",
                schema: "dbo",
                table: "drop_logs",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_drop_logs_Barcode_DropTimeLocal",
                schema: "dbo",
                table: "drop_logs",
                columns: new[] { "Barcode", "DropTimeLocal" });

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
                name: "IX_drop_logs_TaskCode_DropTimeLocal",
                schema: "dbo",
                table: "drop_logs",
                columns: new[] { "TaskCode", "DropTimeLocal" });

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

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_Barcode",
                schema: "dbo",
                table: "scan_logs",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_Barcode_ScanTimeLocal",
                schema: "dbo",
                table: "scan_logs",
                columns: new[] { "Barcode", "ScanTimeLocal" });

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

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_TaskCode",
                schema: "dbo",
                table: "scan_logs",
                column: "TaskCode");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_TaskCode_ScanTimeLocal",
                schema: "dbo",
                table: "scan_logs",
                columns: new[] { "TaskCode", "ScanTimeLocal" });

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

            migrationBuilder.CreateIndex(
                name: "IX_sync_batches_BatchId",
                schema: "dbo",
                table: "sync_batches",
                column: "BatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sync_batches_Status_CompletedTimeLocal",
                schema: "dbo",
                table: "sync_batches",
                columns: new[] { "Status", "CompletedTimeLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_sync_batches_TableCode_Status_CompletedTimeLocal",
                schema: "dbo",
                table: "sync_batches",
                columns: new[] { "TableCode", "Status", "CompletedTimeLocal" });

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
                name: "business_tasks",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "drop_logs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IDX_PICKTOLIGHT_CARTON1",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "IDX_PICKTOWCS2",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "scan_logs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "sorting_task_trace",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "sync_batches",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "sync_change_logs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "sync_deletion_logs",
                schema: "dbo");
        }
    }
}
