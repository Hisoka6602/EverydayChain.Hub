# EverydayChain.Hub

## 本次更新内容
- 启动“分阶段执行 PR 与验收门禁（严格版）”补全：完成 PR-01/PR-02 的第一轮代码补齐，统一业务任务模型新增来源类型、尺寸体积重量、扫描次数、回传标记与回传时间等字段。
- 扫描闭环补齐：`TaskExecutionService` 在扫描成功链路写入长宽高/体积/重量并递增 `ScanCount`，继续保持现有扫描上传接口路由与协议不变。
- 回传与异常状态补齐：`WmsFeedbackService`、`FeedbackCompensationService`、`DropFeedbackService`、`RecirculationService` 已同步维护 `IsFeedbackReported`、`FeedbackTimeLocal`、`IsException` 等字段语义。
- 新增领域枚举 `BusinessTaskSourceType` 与迁移 `20260417043253_AddBusinessTaskClosureFields`，并更新 `HubDbContextModelSnapshot`。
- 新增日志表自动删除配置能力：`RetentionJob.LogTables` 支持对 `sorting_task_trace`、`scan_logs`、`drop_logs`、`sync_batches` 分别配置 `Enabled/KeepMonths/DryRun/AllowDrop`。
- `RetentionExecutionService` 扩展为“同步表保留期 + 日志表保留期”双入口执行，统一走现有危险动作隔离与回滚脚本生成链路。
- `appsettings.json` 已补齐日志表清理示例配置，支持逐表启停和保留期参数化。
- 将 `SyncBatchRepository` 从文件持久化升级为 SQL Server 持久化分片实现：批次数据写入 `sync_batches_{yyyyMM}`，支持 `Pending/InProgress/Completed/Failed` 状态流转与跨分片查询最近失败批次。
- 新增 `SyncBatchEntity`、`SyncBatchEntityTypeConfiguration` 与迁移 `20260416010041_AddSyncBatchShardTable.cs`，将同步批次纳入自动迁移与自动分表预建链路。
- 分表纳管集合新增 `sync_batches`，启动阶段通过 `AutoMigrationService + ShardTableProvisioner` 自动迁移与自动分表。
- `business_tasks`、`scan_logs`、`drop_logs` 从固定表改为按月分表（`{logical}_{yyyyMM}`）并纳入自动分表预建；对应仓储改为分片写入与跨分片查询。
- 移除批次文件落盘残留配置与自检：删除 `SyncJob.BatchFilePath` 与运行期批次文件探针逻辑。
- 实施 PR-11（补偿重试链路）：新增 `IFeedbackCompensationService` 与 `FeedbackCompensationService`（支持按任务编码重试、按批次重试）；新增 `FeedbackCompensationResult` 结果模型；新增 `FeedbackCompensationJobOptions` 配置实体；新增 `FeedbackCompensationBackgroundWorker` 后台任务并接入 `Program.cs` 与 `ServiceCollectionExtensions.cs`；`appsettings.json` 增加 `FeedbackCompensationJob` 配置节。
- 新增补偿单元测试 `FeedbackCompensationServiceTests`，覆盖批次成功、批次失败、按任务跳过、按任务单条重试四个场景。
- 已完成 PR-16（M4）里程碑收口复核：PR-10~PR-11 异常与补偿链路已完成最终全量审查（规则优先级、补偿幂等、重试上限、审计可追溯），阻塞问题清单结论为“无新增阻塞项”。
- 本轮文档收口：同步实施计划、联调证据与代码检查台账口径，完成 PR-12 与 PR-17 收口相关文档定稿。
- 新增 `.github/workflows/auto-create-pr.yml`：仅在非默认分支推送时触发，自动检查并创建到默认分支的 PR（若同源 PR 已存在则跳过），避免人工漏建 PR。
- 实施同步日志持久化替换：新增 `SyncChangeLogEntity`、`SyncDeletionLogEntity`、`SyncChangeLogRepository`、`SyncDeletionLogRepository` 与迁移 `20260416171508_AddSyncChangeDeletionLogShardTables.cs`，`ISyncChangeLogRepository`/`ISyncDeletionLogRepository` 已切换为 SQL Server 分片持久化实现（`sync_change_logs_{yyyyMM}`、`sync_deletion_logs_{yyyyMM}`）。
- 新增波次清理 API：`WaveCleanupController` 提供 `dry-run` 与正式执行端点，支持按 `WaveCode` 执行清理并返回识别数、清理数与 dry-run 标记。
- 新增波次清理 Host 契约：`WaveCleanupRequest`、`WaveCleanupResponse`，统一波次清理接口输入输出语义并补齐中文注释。
- 扩展波次清理应用服务接口：`IWaveCleanupService` 新增 `DryRunByWaveCodeAsync` 与 `ExecuteByWaveCodeAsync`，支持端点级别区分 dry-run/正式执行。
- 新增 `WaveCleanupControllerTests` 与 `StubWaveCleanupService`，覆盖空波次号校验、dry-run 执行与正式执行的控制器行为。
- 构建验证：`dotnet build EverydayChain.Hub.sln` 与 `dotnet test EverydayChain.Hub.sln --no-build` 均通过（0 Warning 0 Error，156/156 单元测试通过）。
## 后续可完善点
- 根据产线峰值写入量细化各日志表差异化保留月数，并结合容量监控进行滚动调优。
- 开启补偿后台任务：生产环境确认重试节流参数后，将 `FeedbackCompensationJob.Enabled` 置 `true`。

## 解决方案文件树与职责
```text
.
├── .gitattributes
├── .gitignore
├── EverydayChain.Hub.sln
├── README.md
├── EFCore手动迁移操作指南.md
├── 持续运行一年稳定性改造清单.md
├── 监控告警规则基线清单.md
├── 年度维护清单.md
├── 值班处置手册.md
├── 当前程序能力与缺陷分析.md
├── 逐文件代码检查方案.md
├── 逐文件全量审查实施方案.md
├── 逐文件代码检查台账.md
├── Oracle到SQLServer同步架构设计.md
├── Oracle到SQLServer同步实施计划.md
├── 兼容现有实现的可切换同步模式改造分析与执行步骤.md
├── EverydayChain.Hub_详细业务背景开发指令_v2.md
├── EverydayChain.Hub_详细业务背景开发指令_v2_实施计划.md
├── WMS状态语义基线.md
├── 条码规则基线.md
├── 对外API接口基线.md
├── 拆零业务字段语义基线.md
├── 整件业务字段语义基线.md
├── docs
│   └── 联调证据
│       └── PR12-20260416-R1
│           ├── 01-联调执行记录.md
│           ├── 02-关键日志索引.md
│           └── 03-结果汇总.md
├── scripts
│   ├── health-check.sh
│   ├── disaster-recovery.sh
│   └── stability-drill.sh
├── .github
│   ├── copilot-instructions.md
│   ├── DDD分层接口与实现放置规范.md
│   └── workflows
│       ├── copilot-governance.yml
│       └── auto-create-pr.yml
├── EverydayChain.Hub.Domain
│   ├── EverydayChain.Hub.Domain.csproj
│   ├── Abstractions/IEntity.cs
│   ├── Enums/SyncMode.cs
│   ├── Enums/DeletionPolicy.cs
│   ├── Enums/LagControlMode.cs
│   ├── Enums/SyncBatchStatus.cs
│   ├── Enums/SyncChangeOperationType.cs
│   ├── Enums/SyncTablePriority.cs
│   ├── Enums/BusinessTaskStatus.cs
│   ├── Enums/BusinessTaskFeedbackStatus.cs
│   ├── Enums/BarcodeType.cs
│   ├── Enums/BarcodeParseFailureReason.cs
│   ├── Enums/BusinessTaskSourceType.cs
│   ├── Sync/SyncTableDefinition.cs
│   ├── Sync/Models/RemoteStatusConsumeProfile.cs
│   ├── Sync/SyncWindow.cs
│   ├── Sync/SyncCheckpoint.cs
│   ├── Sync/SyncBatchResult.cs
│   ├── Sync/SyncBatch.cs
│   ├── Sync/SyncChangeLog.cs
│   ├── Sync/SyncDeletionLog.cs
│   ├── Aggregates/SortingTaskTraceAggregate/SortingTaskTraceEntity.cs
│   ├── Aggregates/BusinessTaskAggregate/BusinessTaskEntity.cs
│   ├── Aggregates/ScanLogAggregate/ScanLogEntity.cs
│   ├── Aggregates/DropLogAggregate/DropLogEntity.cs
│   ├── Aggregates/WmsPickToWcsAggregate/WmsPickToWcsEntity.cs
│   ├── Aggregates/WmsSplitPickToLightCartonAggregate/WmsSplitPickToLightCartonEntity.cs
│   ├── Aggregates/SyncBatchAggregate/SyncBatchEntity.cs
│   ├── Aggregates/SyncChangeLogAggregate/SyncChangeLogEntity.cs
│   ├── Aggregates/SyncDeletionLogAggregate/SyncDeletionLogEntity.cs
│   ├── Options/AutoTuneOptions.cs
│   ├── Options/DangerZoneOptions.cs
│   ├── Options/OracleOptions.cs
│   ├── Options/SwaggerOptions.cs
│   ├── Options/RetentionJobOptions.cs
│   ├── Options/RetentionLogTableOptions.cs
│   ├── Options/ShardingOptions.cs
│   ├── Options/SyncDeleteOptions.cs
│   ├── Options/SyncJobOptions.cs
│   ├── Options/SyncRetentionOptions.cs
│   ├── Options/SyncTableOptions.cs
│   ├── Options/WmsFeedbackOptions.cs
│   ├── Options/FeedbackCompensationJobOptions.cs
│   ├── Options/ExceptionRuleOptions.cs
│   ├── Options/WaveCleanupRuleOptions.cs
│   ├── Options/MultiLabelRuleOptions.cs
│   ├── Options/RecirculationRuleOptions.cs
│   ├── MultiLabel/MultiLabelDecisionResult.cs
│   └── Recirculation/RecirculationDecisionResult.cs
├── EverydayChain.Hub.Application
│   ├── EverydayChain.Hub.Application.csproj
│   ├── Models/SyncExecutionContext.cs
│   ├── Models/SyncReadRequest.cs
│   ├── Models/SyncReadResult.cs
│   ├── Models/SyncMergeRequest.cs
│   ├── Models/SyncMergeResult.cs
│   ├── Models/SyncDeletionDetectRequest.cs
│   ├── Models/SyncDeletionApplyRequest.cs
│   ├── Models/SyncDeletionExecutionResult.cs
│   ├── Models/SyncDeletionCandidate.cs
│   ├── Models/SyncKeyReadRequest.cs
│   ├── Models/SyncTargetStateRow.cs
│   ├── Models/BusinessTaskMaterializeRequest.cs
│   ├── Models/BarcodeParseResult.cs
│   ├── Models/ScanUploadApplicationRequest.cs
│   ├── Models/ScanUploadApplicationResult.cs
│   ├── Models/ChuteResolveApplicationRequest.cs
│   ├── Models/ChuteResolveApplicationResult.cs
│   ├── Models/DropFeedbackApplicationRequest.cs
│   ├── Models/DropFeedbackApplicationResult.cs
│   ├── Models/ScanMatchResult.cs
│   ├── Models/TaskExecutionResult.cs
│   ├── Models/WmsFeedbackApplicationResult.cs
│   ├── Models/FeedbackCompensationResult.cs
│   ├── Abstractions/Persistence/ISyncTaskConfigRepository.cs
│   ├── Abstractions/Persistence/IOracleSourceReader.cs
│   ├── Abstractions/Persistence/ISyncStagingRepository.cs
│   ├── Abstractions/Persistence/ISyncUpsertRepository.cs
│   ├── Abstractions/Persistence/ISyncCheckpointRepository.cs
│   ├── Abstractions/Persistence/ISyncBatchRepository.cs
│   ├── Abstractions/Persistence/ISyncChangeLogRepository.cs
│   ├── Abstractions/Persistence/ISyncDeletionRepository.cs
│   ├── Abstractions/Persistence/ISyncDeletionLogRepository.cs
│   ├── Abstractions/Persistence/IShardTableResolver.cs
│   ├── Abstractions/Persistence/IShardRetentionRepository.cs
│   ├── Abstractions/Persistence/IBusinessTaskRepository.cs
│   ├── Abstractions/Persistence/IScanLogRepository.cs
│   ├── Abstractions/Persistence/IDropLogRepository.cs
│   ├── Abstractions/Sync/IOracleRemoteStatusWriter.cs
│   ├── Abstractions/Sync/IOracleStatusDrivenSourceReader.cs
│   ├── Abstractions/Sync/ISqlServerAppendOnlyWriter.cs
│   ├── Abstractions/Sync/IRemoteStatusConsumeService.cs
│   ├── Abstractions/Services/ISyncOrchestrator.cs
│   ├── Abstractions/Services/ISyncWindowCalculator.cs
│   ├── Abstractions/Services/ISyncExecutionService.cs
│   ├── Abstractions/Services/IDeletionExecutionService.cs
│   ├── Abstractions/Services/IRetentionExecutionService.cs
│   ├── Abstractions/Services/IBusinessTaskMaterializer.cs
│   ├── Abstractions/Services/IBarcodeParser.cs
│   ├── Abstractions/Services/IScanMatchService.cs
│   ├── Abstractions/Services/ITaskExecutionService.cs
│   ├── Abstractions/Services/IScanIngressService.cs
│   ├── Abstractions/Services/IChuteQueryService.cs
│   ├── Abstractions/Services/IDropFeedbackService.cs
│   ├── Abstractions/Services/IWmsFeedbackService.cs
│   ├── Abstractions/Services/IFeedbackCompensationService.cs
│   ├── Abstractions/Integrations/IWmsOracleFeedbackGateway.cs
│   ├── WaveCleanup/Abstractions/IWaveCleanupService.cs
│   ├── WaveCleanup/Abstractions/WaveCleanupResult.cs
│   ├── WaveCleanup/Services/WaveCleanupService.cs
│   ├── MultiLabel/Abstractions/IMultiLabelDecisionService.cs
│   ├── MultiLabel/Services/MultiLabelDecisionService.cs
│   ├── Recirculation/Abstractions/IRecirculationService.cs
│   ├── Recirculation/Services/RecirculationService.cs
│   ├── Models/RemoteStatusConsumeResult.cs
│   ├── Services/SyncOrchestrator.cs
│   ├── Services/SyncWindowCalculator.cs
│   ├── Services/SyncExecutionService.cs
│   ├── Services/BusinessTaskMaterializer.cs
│   ├── Services/BarcodeParser.cs
│   ├── ScanMatch/Services/ScanMatchService.cs
│   ├── TaskExecution/Services/TaskExecutionService.cs
│   ├── Services/ScanIngressService.cs
│   ├── Services/ChuteQueryService.cs
│   ├── Services/DropFeedbackService.cs
│   ├── Feedback/Services/WmsFeedbackService.cs
│   ├── Feedback/Services/FeedbackCompensationService.cs
│   ├── Services/DeletionExecutionService.cs
│   └── Services/RetentionExecutionService.cs
├── EverydayChain.Hub.SharedKernel
│   ├── EverydayChain.Hub.SharedKernel.csproj
│   └── Utilities
│       ├── LogicalTableNameNormalizer.cs
│       ├── RuntimeStoragePathResolver.cs
│       ├── BoundedConcurrentQueueHelper.cs
│       ├── SyncBusinessKeyBuilder.cs
│       ├── SyncColumnFilter.cs
│       ├── LocalDateTimeNormalizer.cs
│       └── TaskCodeNormalizer.cs
├── EverydayChain.Hub.Infrastructure
│   ├── EverydayChain.Hub.Infrastructure.csproj
│   ├── DependencyInjection/ServiceCollectionExtensions.cs
│   ├── Properties/AssemblyInfo.cs
│   ├── Sync/Readers/OracleStatusDrivenSourceReader.cs
│   ├── Sync/Writers/SqlServerAppendOnlyWriter.cs
│   ├── Sync/Writers/OracleRemoteStatusWriter.cs
│   ├── Sync/Services/RemoteStatusConsumeService.cs
│   ├── Integrations/OracleWmsFeedbackGateway.cs
│   ├── Repositories/SyncTaskConfigRepository.cs
│   ├── Repositories/OracleSourceReader.cs
│   ├── Repositories/SyncStagingRepository.cs
│   ├── Repositories/SqlServerSyncUpsertRepository.cs
│   ├── Repositories/SyncDeletionRepository.cs
│   ├── Repositories/ShardTableResolver.cs
│   ├── Repositories/ShardRetentionRepository.cs
│   ├── Repositories/SyncCheckpointRepository.cs
│   ├── Repositories/SyncBatchRepository.cs
│   ├── Repositories/SyncChangeLogRepository.cs
│   ├── Repositories/SyncDeletionLogRepository.cs
│   ├── Repositories/BusinessTaskRepository.cs
│   ├── Repositories/ScanLogRepository.cs
│   ├── Repositories/DropLogRepository.cs
│   ├── Persistence/HubDbContext.cs
│   ├── Persistence/DesignTimeHubDbContextFactory.cs
│   ├── Persistence/EntityConfigurations/SortingTaskTraceEntityTypeConfiguration.cs
│   ├── Persistence/EntityConfigurations/BusinessTaskEntityTypeConfiguration.cs
│   ├── Persistence/EntityConfigurations/ScanLogEntityTypeConfiguration.cs
│   ├── Persistence/EntityConfigurations/DropLogEntityTypeConfiguration.cs
│   ├── Persistence/EntityConfigurations/SyncBatchEntityTypeConfiguration.cs
│   ├── Persistence/EntityConfigurations/SyncChangeLogEntityTypeConfiguration.cs
│   ├── Persistence/EntityConfigurations/SyncDeletionLogEntityTypeConfiguration.cs
│   ├── Persistence/Sharding/TableSuffixScope.cs
│   ├── Persistence/Sharding/IShardSuffixResolver.cs
│   ├── Persistence/Sharding/MonthShardSuffixResolver.cs
│   ├── Persistence/Sharding/ShardModelCacheKeyFactory.cs
│   ├── Migrations/20260408020833_RebuildInitialHubSchema.cs
│   ├── Migrations/20260408020833_RebuildInitialHubSchema.Designer.cs
│   ├── Migrations/20260413144042_AddBusinessTaskTable.cs
│   ├── Migrations/20260413144042_AddBusinessTaskTable.Designer.cs
│   ├── Migrations/20260413160852_AddScanDropLogTables.cs
│   ├── Migrations/20260413160852_AddScanDropLogTables.Designer.cs
│   ├── Migrations/20260416010041_AddSyncBatchShardTable.cs
│   ├── Migrations/20260416171508_AddSyncChangeDeletionLogShardTables.cs
│   ├── Migrations/20260417043253_AddBusinessTaskClosureFields.cs
│   ├── Migrations/20260417043253_AddBusinessTaskClosureFields.Designer.cs
│   ├── Migrations/HubDbContextModelSnapshot.cs
│   └── Services
│       ├── IDangerZoneExecutor.cs
│       ├── DangerZoneExecutor.cs
│       ├── IRuntimeStorageGuard.cs
│       ├── RuntimeStorageGuard.cs
│       ├── IAutoMigrationService.cs
│       ├── AutoMigrationService.cs
│       ├── IShardTableProvisioner.cs
│       ├── ShardTableProvisioner.cs
│       ├── ISqlExecutionTuner.cs
│       ├── SqlExecutionTuner.cs
│       ├── ISortingTaskTraceWriter.cs
│       ├── SortingTaskTraceWriter.cs
│       ├── NonRetryableDangerZoneException.cs
│       └── OracleConnectionStringResolver.cs
├── EverydayChain.Hub.Tests
│   ├── EverydayChain.Hub.Tests.csproj
│   ├── Repositories/OracleSourceReaderTests.cs
│   ├── Repositories/InMemorySqlServerSyncUpsertRepository.cs
│   ├── Repositories/NoOpShardTableProvisioner.cs
│   ├── Repositories/SyncStagingRepositoryTests.cs
│   ├── Repositories/SqlServerSyncUpsertRepositoryTests.cs
│   ├── Repositories/SyncTaskConfigRepositoryTests.cs
│   ├── Sync/RemoteStatusConsumeServiceTests.cs
│   ├── Sync/Fakes/FakeOracleStatusDrivenSourceReader.cs
│   ├── Sync/Fakes/FakeSqlServerAppendOnlyWriter.cs
│   ├── Sync/Fakes/FakeOracleRemoteStatusWriter.cs
│   ├── Host/Controllers/ScanControllerTests.cs
│   ├── Host/Controllers/ChuteControllerTests.cs
│   ├── Host/Controllers/DropFeedbackControllerTests.cs
│   ├── Host/Controllers/WaveCleanupControllerTests.cs
│   ├── Host/Controllers/StubScanIngressService.cs
│   ├── Host/Controllers/StubChuteQueryService.cs
│   ├── Host/Controllers/StubDropFeedbackService.cs
│   ├── Host/Controllers/StubWaveCleanupService.cs
│   └── Services
│       ├── AutoMigrationServiceTests.cs
│       ├── DangerZoneExecutorTests.cs
│       ├── FixedBootstrapShardSuffixResolver.cs
│       ├── HubDbContextTestFactory.cs
│       ├── LoggerNullScope.cs
│       ├── PassThroughSqlExecutionTuner.cs
│       ├── RecordingShardTableProvisioner.cs
│       ├── ServiceCollectionExtensionsTests.cs
│       ├── BusinessTaskMaterializerTests.cs
│       ├── BarcodeParserTests.cs
│       ├── ScanIngressServiceTests.cs
│       ├── ScanMatchServiceTests.cs
│       ├── TaskExecutionServiceTests.cs
│       ├── ChuteQueryServiceTests.cs
│       ├── DropFeedbackServiceTests.cs
│       ├── WmsFeedbackServiceTests.cs
│       ├── FeedbackCompensationServiceTests.cs
│       ├── ScanDropLogTests.cs
│       ├── InMemoryScanLogRepository.cs
│       ├── InMemoryDropLogRepository.cs
│       ├── ShardTableProvisionerTests.cs
│       ├── SortingTaskTraceWriterTests.cs
│       ├── LocalDateTimeNormalizerTests.cs
│       ├── TestLogger.cs
│       ├── ThrowingHubDbContextFactory.cs
│       └── SyncWindowCalculatorTests.cs
└── EverydayChain.Hub.Host
    ├── EverydayChain.Hub.Host.csproj
    ├── Program.cs
    ├── Controllers/ScanController.cs
    ├── Controllers/ChuteController.cs
    ├── Controllers/DropFeedbackController.cs
    ├── Controllers/WaveCleanupController.cs
    ├── Contracts/Requests/ScanUploadRequest.cs
    ├── Contracts/Requests/ChuteResolveRequest.cs
    ├── Contracts/Requests/DropFeedbackRequest.cs
    ├── Contracts/Requests/WaveCleanupRequest.cs
    ├── Contracts/Responses/ApiResponse.cs
    ├── Contracts/Responses/ScanUploadResponse.cs
    ├── Contracts/Responses/ChuteResolveResponse.cs
    ├── Contracts/Responses/DropFeedbackResponse.cs
    ├── Contracts/Responses/WaveCleanupResponse.cs
    ├── Workers/SyncBackgroundWorker.cs
    ├── Workers/RetentionBackgroundWorker.cs
    ├── Workers/AutoMigrationHostedService.cs
    ├── Workers/FeedbackCompensationBackgroundWorker.cs
    ├── Properties/launchSettings.json
    ├── nlog.config
    ├── appsettings.json
    ├── install.bat
    └── uninstall.bat
```

## 各层级与各文件作用说明（逐项）
- `.gitattributes`：设置 Git 仓库行尾统一处理规则（`text=auto`），确保跨平台提交行尾一致性。
- `.gitignore`：配置 Visual Studio 及 .NET 项目的忽略规则，排除构建产物（`bin/`、`obj/`）、用户配置（`*.suo`、`*.user`）与临时文件。
- `.github/copilot-instructions.md`：定义仓库级 Copilot 强制约束，覆盖时间语义、结构规范、文档联动与交付门禁。
- `.github/DDD分层接口与实现放置规范.md`：DDD 项目接口定义位置、实现类放置位置、依赖方向与目录结构统一规范，覆盖领域/应用/基础设施各层的抽象归属规则。
- `.github/workflows/copilot-governance.yml`：执行规则自动校验，并强制规则文件与工作流联动修改。
- `.github/workflows/auto-create-pr.yml`：在非默认分支发生推送时自动检查同源分支 PR 是否存在，若不存在则自动创建到默认分支的 PR。
- `scripts/health-check.sh`：一键体检脚本，检查磁盘空间、目录权限、关键文件可读写、配置文件格式、日志健康状态、进程存活与压缩归档文件状态，可集成到监控或定时任务。
- `scripts/disaster-recovery.sh`：灾难恢复脚本，支持检查点重置（checkpoint-reset）、快照从归档恢复（snapshot-restore）、快照备份（snapshot-backup）、归档清理（archive-cleanup）与完全重置（full-reset）；全部操作支持 --dry-run 预览模式。
- `scripts/stability-drill.sh`：稳定性演练脚本，串联体检与灾备动作（checkpoint-reset、snapshot-backup、snapshot-restore、archive-cleanup），支持 dry-run 与真实执行并自动生成演练记录。
- `docs/联调证据/PR12-20260416-R1/01-联调执行记录.md`：PR-12 联调收口 R1 批次执行记录，归档本地时间窗口、回归命令与端到端链路执行状态。
- `docs/联调证据/PR12-20260416-R1/02-关键日志索引.md`：PR-12 联调收口 R1 批次关键日志索引，固化日志范围、检索词口径与命中补录表。
- `docs/联调证据/PR12-20260416-R1/03-结果汇总.md`：PR-12 联调收口 R1 批次结果汇总，记录统计口径、回归结果与最终收口结论。
- `监控告警规则基线清单.md`：监控告警规则基线文档，定义日志关键字告警、指标阈值告警与演练留档验收口径，用于补齐稳定性清单剩余交付项。
- `年度维护清单.md`：月度/季度/年度例行巡检项标准化清单，包含磁盘治理、日志审查、数据一致性、配置审核、灾难恢复演练、容量规划、安全审计等条目及快速异常处理参考表。
- `值班处置手册.md`：日常值班与告警应急处置手册，覆盖 9 类告警的处置步骤（卡死检测、磁盘不足、内存水位、整轮超时、熔断、检查点损坏、快照损坏、归档失败、进程停止），定义 P0~P3 优先级与升级规则，含处置记录与演练记录模板。
- `逐文件代码检查方案.md`：逐文件审查执行方案，定义检查范围、单文件检查维度、无遗漏对账流程、问题分级与分批 PR 策略，支持“本 PR 不改代码”的审查场景。
- `逐文件全量审查实施方案.md`：续审执行方案，要求先核对首轮已处理内容，再对首轮台账未覆盖文件执行补审并闭环。
- `逐文件代码检查台账.md`：逐文件检查台账（首轮 155 文件 + 续审批次 A 补齐 15 文件），记录每文件检查状态（未检查/进行中/已完成）、问题编号与修复状态，供后续 PR 复核追溯。
- `WMS状态语义基线.md`：固化读取状态、回传状态、自动回写字段与业务回传字段边界，统一同步链路与业务链路语义。
- `条码规则基线.md`：固化条码输入约束、固定匹配规则（拆零 `02` 开头取第 3 位格口号 / 整件 `Z` 开头取第 2 位格口号）、解析输出与失败语义，约束扫描链路判定口径。
- `对外API接口基线.md`：固化扫描上传、请求格口、落格回传 3 类接口的路由、方法、入参出参、成功失败语义、幂等要求与状态变化。
- `拆零业务字段语义基线.md`：固化拆零业务任务关键字段语义与状态推进约束，避免链路间字段理解偏差。
- `整件业务字段语义基线.md`：固化整件业务任务关键字段语义与状态推进约束，统一与拆零路径的状态机口径。
- `SyncTableDefinition.cs` / `SyncWindow.cs` / `SyncCheckpoint.cs` / `SyncBatchResult.cs`：定义同步链路执行、窗口与结果统计的核心领域模型。
- `SyncBatch.cs` / `SyncChangeLog.cs` / `SyncDeletionLog.cs`：定义批次状态跟踪、变更审计与删除审计的数据模型。
- `SyncBusinessKeyBuilder.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：同步业务键构建共享组件，按 `UniqueKeys` 配置将行数据拼接为 `|` 分隔的业务键文本，供 Upsert 与删除识别阶段统一调用。
- `SyncColumnFilter.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：同步列过滤共享组件，提供 `ExcludedColumns` 规范化与行级过滤能力，并统一维护软删除关键列常量。
- `RuntimeStoragePathResolver.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：运行期路径解析共享组件，统一解析检查点、目标快照与存储守护所需的绝对路径。
- `LogicalTableNameNormalizer.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：逻辑表名规范化与安全校验共享组件，统一执行去空白、SQL 标识符校验与异常信息输出。
- `BoundedConcurrentQueueHelper.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：有界并发队列淘汰辅助工具，仅执行一次 O(n) `Count` 遍历并缓存结果，供需要内存容量保护的队列实现统一复用。
- `LocalDateTimeNormalizer.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：本地时间规范化共享工具，统一执行 UTC 拒绝、`MinValue` 回退当前本地时间与 `Unspecified` 转本地时间语义，供 Host API 复用。
- `TaskCodeNormalizer.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：任务编码规范化共享工具，统一执行去首尾空白与全空白回退空字符串处理，供 Host API 复用。
- `SyncMode.cs` / `DeletionPolicy.cs` / `LagControlMode.cs` / `SyncBatchStatus.cs` / `SyncChangeOperationType.cs` / `SyncTablePriority.cs` / `BarcodeType.cs` / `BarcodeParseFailureReason.cs` / `BusinessTaskSourceType.cs`：同步模式、删除策略、滞后控制、批次状态、变更操作类型、调度优先级、条码解析语义与任务来源类型枚举，均含中文 XML 注释与 `Description`。
- `BusinessTaskStatus.cs`：业务任务生命周期状态枚举，覆盖 Created、Scanned、Dropped、FeedbackPending、Exception，并提供中文 `Description` 说明。
- `BusinessTaskFeedbackStatus.cs`：业务回传状态枚举，覆盖 NotRequired、Pending、Completed、Failed，标识任务回传 WMS 的进度。
- `RemoteStatusConsumeProfile.cs`（`EverydayChain.Hub.Domain/Sync/Models`）：StatusDriven 消费配置模型，统一承载状态列、待处理值、完成值、回写开关与批次大小。
- `EverydayChain.Hub.Domain/Options/*.cs`：统一承载全部配置实体（`Sharding`、`AutoTune`、`DangerZone`、`SyncJob`、`SyncTable`、`SyncDelete`、`SyncRetention`、`RetentionJob`、`Oracle` 等），供 Infrastructure 绑定读取。
- `Domain/Options/RetentionLogTableOptions.cs`：日志表保留期配置实体，定义单日志表 `Enabled`、`LogicalTableName`、`KeepMonths`、`DryRun`、`AllowDrop` 参数。
- `SwaggerOptions.cs`：Swagger 文档配置实体，承载标题、版本、描述与各环境开关（开发/测试/生产）。
- `SortingTaskTraceEntity.cs`：可分表的写入实体，承载中台追踪数据；所有属性均含 XML 注释。
- `BusinessTaskEntity.cs`（`Domain/Aggregates/BusinessTaskAggregate`）：统一业务任务聚合根实体，承载任务编码、来源表、来源类型、业务键、条码、目标格口、实际格口、设备编码、链路追踪、失败原因、扫描时间、落格时间、尺寸体积重量、扫描次数、回传标记、回传时间、波次信息、异常/回流标记、任务状态、回传状态与本地时间字段。
- `SyncExecutionContext.cs` + `SyncReadRequest.cs` + `SyncReadResult.cs` + `SyncMergeRequest.cs` + `SyncMergeResult.cs` + `SyncDeletionDetectRequest.cs` + `SyncDeletionApplyRequest.cs` + `SyncDeletionExecutionResult.cs` + `SyncDeletionCandidate.cs` + `SyncKeyReadRequest.cs` + `SyncTargetStateRow.cs`：同步执行、删除识别与轻量幂等状态存储的数据契约模型。
- `BusinessTaskMaterializeRequest.cs`：业务任务物化输入模型，统一约束任务编码、来源表编码、业务键、条码、来源类型、波次信息与物化时间字段。
- `ScanUploadApplicationRequest.cs` / `ScanUploadApplicationResult.cs` / `BarcodeParseResult.cs` / `ChuteResolveApplicationRequest.cs` / `ChuteResolveApplicationResult.cs` / `DropFeedbackApplicationRequest.cs` / `DropFeedbackApplicationResult.cs` / `ScanMatchResult.cs` / `TaskExecutionResult.cs`：扫描、格口、落格链路的应用层输入输出模型；`DropFeedbackApplicationRequest` 新增 `IsSuccess`、`FailureReason`；`ScanMatchResult` 与 `TaskExecutionResult` 为 PR-05 新增的中间结果模型。
- `Application/Abstractions/Services/IBusinessTaskMaterializer.cs` + `Application/Services/BusinessTaskMaterializer.cs`：业务任务物化服务抽象与实现，仅执行字段映射、文本规范化和默认状态赋值，不承载扫描/格口/落格业务规则。
- `Application/Abstractions/Persistence/IBusinessTaskRepository.cs`：业务任务仓储抽象，定义按条码、任务编码、主键查询及新增、更新操作契约。
- `Application/Abstractions/Services/IScanMatchService.cs`：扫描匹配服务抽象，按条码定位关联业务任务并返回匹配结果。
- `Application/Abstractions/Services/ITaskExecutionService.cs`：任务执行服务抽象，负责推进业务任务扫描状态并持久化。
- `Application/ScanMatch/Services/ScanMatchService.cs`：扫描匹配服务实现，按条码在业务任务仓储中定位任务。
- `Application/TaskExecution/Services/TaskExecutionService.cs`：任务执行服务实现，按条码匹配任务、校验状态并推进到已扫描并持久化，同时写入扫描维度字段（时间、尺寸体积重量、扫描次数）。
- `Infrastructure/Repositories/BusinessTaskRepository.cs`：业务任务仓储 EF Core 实现，按月分片写入与查询 `business_tasks_{yyyyMM}`，并兼容历史无后缀表读取。
- `Infrastructure/Persistence/EntityConfigurations/BusinessTaskEntityTypeConfiguration.cs`：业务任务 EF Fluent API 配置，定义分片表结构、字段约束与索引。
- `Application/Abstractions/Services/IBarcodeParser.cs` + `Application/Services/BarcodeParser.cs`：条码解析服务抽象与实现，按固定规则“拆零 `02` 开头取第 3 位数字、整件 `Z` 开头取第 2 位数字”分类并提取 `TargetChuteCode`，统一输出失败语义（InvalidBarcode、UnsupportedBarcodeType、ParseError）。
- `Application/Abstractions/Services/IScanIngressService.cs` + `Application/Services/ScanIngressService.cs`：扫描上传应用服务，协调条码解析、任务匹配与状态推进链路，输出标准化受理结果。
- `Application/Abstractions/Services/IChuteQueryService.cs` + `Application/Services/ChuteQueryService.cs`：请求格口应用服务抽象与实现，按任务编码或条码查询业务任务，在任务已扫描前提下按条码规则解析并返回目标格口，覆盖状态校验与不支持条码异常分支。
- `Application/Abstractions/Services/IDropFeedbackService.cs` + `Application/Services/DropFeedbackService.cs`：落格回传应用服务抽象与实现，支持双定位（TaskCode/Barcode）、参数冲突校验与状态机推进（成功→Dropped+FeedbackPending，失败→Exception），落格成功/失败均写落格日志。
- `Application/Abstractions/Services/IWmsFeedbackService.cs` + `Application/Feedback/Services/WmsFeedbackService.cs`：业务回传应用服务抽象与实现，查询 `FeedbackStatus=Pending` 任务、批量调用 Oracle 写入器、按结果回填 Completed/Failed。
- `Application/Abstractions/Services/IFeedbackCompensationService.cs` + `Application/Feedback/Services/FeedbackCompensationService.cs`：业务回传补偿服务抽象与实现，支持按任务编码重试与按批次重试 `FeedbackStatus=Failed` 任务，并回填本地回传状态。
- `Application/Abstractions/Integrations/IWmsOracleFeedbackGateway.cs` + `Infrastructure/Integrations/OracleWmsFeedbackGateway.cs`：Oracle WMS 业务回传网关抽象与实现；实现使用数组绑定批量更新，安全标识符校验防止 SQL 注入；`Enabled=false` 时仅记录日志不实际写入 Oracle。
- `Domain/Options/WmsFeedbackOptions.cs`：业务回传配置实体，定义 Schema、Table、BusinessKeyColumn、FeedbackStatusColumn、FeedbackCompletedValue、FeedbackTimeColumn、ActualChuteColumn、CommandTimeoutSeconds 与 Enabled 开关（默认 false）。
- `Domain/Options/FeedbackCompensationJobOptions.cs`：业务回传补偿后台任务配置实体，定义补偿开关、轮询间隔与每轮批次上限。
- `Application/Models/WmsFeedbackApplicationResult.cs`：业务回传执行结果模型，汇总 PendingCount、SuccessCount、FailedCount 与 IsSuccess。
- `Application/Models/FeedbackCompensationResult.cs`：业务回传补偿执行结果模型，汇总目标数量、重试数量、成功/失败/跳过数量与失败原因。
- `Application/Abstractions/Persistence/IScanLogRepository.cs` + `Infrastructure/Repositories/ScanLogRepository.cs`：扫描日志仓储抽象与 EF Core 实现，按月写入 `scan_logs_{yyyyMM}`。
- `Application/Abstractions/Persistence/IDropLogRepository.cs` + `Infrastructure/Repositories/DropLogRepository.cs`：落格日志仓储抽象与 EF Core 实现，按月写入 `drop_logs_{yyyyMM}`。
- `Domain/Aggregates/ScanLogAggregate/ScanLogEntity.cs`：扫描日志聚合实体，记录条码、匹配结果、失败原因、设备编码、链路追踪、扫描时间等审计字段。
- `Domain/Aggregates/DropLogAggregate/DropLogEntity.cs`：落格日志聚合实体，记录任务编码、条码、实际格口、成功标志、失败原因、落格时间等审计字段。
- `Domain/Aggregates/SyncChangeLogAggregate/SyncChangeLogEntity.cs`：同步变更日志聚合实体，记录批次、表编码、操作类型、业务键、变更快照与本地时间。
- `Domain/Aggregates/SyncDeletionLogAggregate/SyncDeletionLogEntity.cs`：同步删除日志聚合实体，记录批次、删除策略、执行标识、删除时间与源端证据。
- `Infrastructure/Persistence/EntityConfigurations/ScanLogEntityTypeConfiguration.cs`：扫描日志 EF Fluent API 配置，定义分片表结构、字段约束与查询索引。
- `Infrastructure/Persistence/EntityConfigurations/DropLogEntityTypeConfiguration.cs`：落格日志 EF Fluent API 配置，定义分片表结构、字段约束与查询索引。
- `Infrastructure/Persistence/EntityConfigurations/SyncChangeLogEntityTypeConfiguration.cs`：同步变更日志 EF Fluent API 配置，定义分片表结构、字段约束与查询索引。
- `Infrastructure/Persistence/EntityConfigurations/SyncDeletionLogEntityTypeConfiguration.cs`：同步删除日志 EF Fluent API 配置，定义分片表结构、字段约束与查询索引。
- `20260413160852_AddScanDropLogTables.cs`：新增 `scan_logs` 与 `drop_logs` 表的 EF 迁移，包含所有字段与索引定义。
- `EverydayChain.Hub.Tests/Services/WmsFeedbackServiceTests.cs`：业务回传服务单元测试，覆盖无待回传任务空结果、写入成功置 Completed、写入器异常置 Failed、batchSize 限制、Enabled=false 直接短路、writtenRows 不一致整批失败六个场景；含 `CapturingWmsOracleFeedbackGateway` 捕获替身。
- `Domain/Options/ExceptionRuleOptions.cs`：异常规则根配置实体，包含全局开关与 dry-run 标志，持有三个子配置实例。
- `Domain/Options/WaveCleanupRuleOptions.cs`：波次清理子配置，定义启用开关与 `TargetStatusOnCleanup`（可填项：Exception）。
- `Domain/Options/MultiLabelRuleOptions.cs`：多标签决策子配置，定义启用开关与策略（可填项：UseFirst、UseLatest、MarkException）。
- `Domain/Options/RecirculationRuleOptions.cs`：回流规则子配置，定义启用开关与 `MaxScanRetries`（可填范围：1~100）。
- `Domain/MultiLabel/MultiLabelDecisionResult.cs`：多标签决策结果领域模型，包含是否多标签、决策状态、选用/舍弃任务编码与推荐目标状态。
- `Domain/Recirculation/RecirculationDecisionResult.cs`：回流决策结果领域模型，包含是否需要回流、触发原因、扫描重试次数与推荐目标状态。
- `Application/WaveCleanup/Abstractions/IWaveCleanupService.cs`：波次清理服务接口，声明按波次编码清理非终态任务的契约。
- `Application/WaveCleanup/Abstractions/WaveCleanupResult.cs`：波次清理结果模型，包含识别数、清理数、dry-run 标志与执行摘要。
- `Application/WaveCleanup/Services/WaveCleanupService.cs`：波次清理服务实现，支持 dry-run 模式并使用单次批量更新避免 N+1。
- `Application/MultiLabel/Abstractions/IMultiLabelDecisionService.cs`：多标签决策服务接口，对给定条码的所有关联活跃任务执行策略决策。
- `Application/MultiLabel/Services/MultiLabelDecisionService.cs`：多标签决策服务实现，支持 UseFirst/UseLatest/MarkException 三种策略。
- `Application/Recirculation/Abstractions/IRecirculationService.cs`：回流规则服务接口，对指定任务执行回流判定。
- `Application/Recirculation/Services/RecirculationService.cs`：回流规则服务实现，按扫描重试次数超限判定回流，支持 dry-run 模式。
- `20260413185000_AddExceptionRuleFields.cs`：新增 `WaveCode`、`IsRecirculated`、`ScanRetryCount` 列及 `IX_business_tasks_WaveCode` 索引的 EF 迁移。
- `20260417043253_AddBusinessTaskClosureFields.cs`：新增 `SourceType`、尺寸体积重量、`ScanCount`、`IsException`、`IsFeedbackReported`、`FeedbackTimeLocal`、`WaveRemark` 字段及配套索引的 EF 迁移。
- `EverydayChain.Hub.Tests/Services/ExceptionRuleTests.cs`：异常规则服务单元测试，覆盖波次清理、多标签决策与回流规则的主要路径共 16 个场景。
- `EverydayChain.Hub.Tests/Services/ScanDropLogTests.cs`：扫描/落格日志落库测试，覆盖扫描成功写日志、扫描失败写日志、落格成功写日志+FeedbackPending、落格失败写日志四个场景。
- `EverydayChain.Hub.Tests/Services/InMemoryScanLogRepository.cs`：扫描日志仓储内存替身。
- `EverydayChain.Hub.Tests/Services/InMemoryDropLogRepository.cs`：落格日志仓储内存替身。
- `Application/Abstractions/Sync/IOracleRemoteStatusWriter.cs` / `IOracleStatusDrivenSourceReader.cs` / `ISqlServerAppendOnlyWriter.cs`：定义 StatusDriven 模式中 Oracle 远端状态回写、Oracle 状态驱动源读取与 SQL Server 仅追加写入的外部协作能力抽象，遵循 Application 层外部协作抽象放置规则。
- `Application/Abstractions/Sync/IRemoteStatusConsumeService.cs` + `Application/Models/RemoteStatusConsumeResult.cs`：定义 StatusDriven 模式执行入口（应用编排抽象）与读取/追加/回写统计模型。
- `Application/Abstractions/Persistence/ISyncBatchRepository.cs` / `ISyncChangeLogRepository.cs` / `ISyncDeletionRepository.cs` / `ISyncDeletionLogRepository.cs`：定义批次状态、变更日志、删除识别执行与删除日志写入契约。
- `Application/Abstractions/Persistence/IShardTableResolver.cs` / `IShardRetentionRepository.cs`：定义分表识别与分表清理执行契约（含分表完整回滚脚本生成）。
- `Application/Abstractions/Services/ISyncOrchestrator.cs` / `SyncOrchestrator.cs`：同步任务编排入口应用抽象（位于 Abstractions/Services/）；`SyncOrchestrator.cs` 实现位于 Services/ 目录。
- `Application/Abstractions/Services/ISyncWindowCalculator.cs` / `SyncWindowCalculator.cs`：根据 `CursorColumn + StartTimeLocal` 与检查点计算本地增量窗口，并对时钟回拨与 DST 非法本地时刻执行窗口边界保护。
- `Application/Abstractions/Services/IDeletionExecutionService.cs` / `DeletionExecutionService.cs`：执行删除识别、删除策略应用（含 DryRun）并生成删除审计与删除变更日志。
- `Application/Abstractions/Services/IRetentionExecutionService.cs` / `RetentionExecutionService.cs`：执行分表保留期治理，完成过期分表识别、完整回滚脚本生成、dry-run 审计、删除执行、失败隔离与汇总。
- `Application/Abstractions/Services/ISyncExecutionService.cs` / `SyncExecutionService.cs`：执行分页读取、暂存、幂等合并、删除同步、日志写入、检查点提交，并输出延迟/积压/吞吐/失败率指标日志；异常场景输出 NLog 错误日志。
- `HubDbContext.cs`：根据分表后缀动态映射表名。
- `TableSuffixScope.cs` + `ShardModelCacheKeyFactory.cs`：保证不同后缀下 EF Model 能正确缓存隔离。
- `MonthShardSuffixResolver.cs`：按月份生成分表后缀（如 `_202603`）。
- `IShardTableProvisioner.cs` + `ShardTableProvisioner.cs`：在 SQL Server 中按需创建分表与索引（不存在才建）。
- `AutoMigrationService.cs`：应用启动时自动建库、自动识别并执行待迁移项，同时仅预建后缀分表（Infrastructure 层实现）。
- `Host/Workers/AutoMigrationHostedService.cs`：后台任务入口，在应用启动阶段触发自动迁移与分表预置流程，依赖 `IAutoMigrationService` 与 `IRuntimeStorageGuard`（遵循后台任务入口归属 Host 层规则）。
- `SqlExecutionTuner.cs`：基于失败率和耗时进行批量窗口升降调谐；采样窗口大小与失败率阈值均来自 `AutoTuneOptions`。
- `DangerZoneExecutor.cs`：危险路径统一走隔离器（超时/重试/熔断），弹性参数来自 `DangerZoneOptions`。
- `NonRetryableDangerZoneException.cs`：危险隔离器“不可重试异常”标记类型，用于识别配置类确定性失败并快速失败。
- `OracleConnectionStringResolver.cs`：Oracle 连接串解析器（Infrastructure 内部工具类），统一处理 `Database`/`DatabaseMode` 对 Data Source 的 EZCONNECT 覆写逻辑；支持 Auto/ServiceName/SID 三种模式，复杂 DESCRIPTION 描述符场景下快速失败。
- `IRuntimeStorageGuard.cs` + `RuntimeStorageGuard.cs`：运行期存储守护服务，负责启动阶段的磁盘空间、目录权限、关键文件可读写自检，并在检查点/目标快照写入前执行磁盘阈值校验与告警阻断；同时提供单表内存水位监控与节流告警能力。
- `SortingTaskTraceWriter.cs`：按分表后缀分组写入，并将执行结果回传给调谐器。
- `SyncTaskConfigRepository.cs`：从 `SyncJob` 配置节读取表定义，校验 `StartTimeLocal` 禁止 `Z` 与 offset，校验 `ExcludedColumns` 不得与 `UniqueKeys`、`CursorColumn`、软删除关键列冲突，并解析优先级与多表并发上限；新增 `SyncMode` 与 StatusDriven 参数映射、默认值填充及中文错误校验。
- `OracleOptions.cs`：远端 Oracle 连接配置实体，定义连接字符串、连接库名（ServiceName/SID，决定连接目标）、只读开关、命令超时与分页上限。
- `OracleSourceReader.cs`：源端读取器 Oracle 实现，使用参数化 SQL 执行真实只读查询，支持分页增量读取、业务键读取、`ExcludedColumns` 过滤，并在异常场景输出错误日志；支持 `Oracle.DatabaseMode` 控制库名拼接语义（ServiceName/SID）。
- `Sync/Readers/OracleStatusDrivenSourceReader.cs`：StatusDriven 读取器，按状态列读取待处理行，支持 `PendingStatusValue=null` 时生成 `IS NULL` 条件，并输出 `__RowId` 供后续回写使用。
- `Sync/Writers/SqlServerAppendOnlyWriter.cs`：StatusDriven 本地落库写入器，仅执行批量追加，不执行 merge/delete。
- `Sync/Writers/OracleRemoteStatusWriter.cs`：StatusDriven 远端回写器，仅按 `ROWID` 更新远端状态列。
- `Sync/Services/RemoteStatusConsumeService.cs`：串联“读取→追加→可选回写”流程，执行页级异常隔离并输出中文统计日志。
- `SyncStagingRepository.cs`：暂存仓储基础实现，按 `BatchId + PageNo` 进行内存暂存，并在写入阶段过滤 `ExcludedColumns`。
- `SqlServerSyncUpsertRepository.cs`：SQL Server 真实落库实现，按目标逻辑表+后缀分表执行集合式 MERGE（支持配置回退逐行模式），并在 `sync_target_state_{tableCode}_{yyyyMM}` 状态分表中记录后缀；读取/删除状态时跨月分表聚合，且兼容旧版 `sync_target_state` / `sync_target_state_{tableCode}` 状态表，确保升级过程幂等与删除语义连续。
- `SyncDeletionRepository.cs`：删除同步仓储基础实现，基于轻量幂等状态执行窗口过滤与源端键差异识别，并按策略执行删除。
- `ShardTableResolver.cs`：分表解析仓储实现，按逻辑表枚举物理分表并解析分表月份后缀。
- `ShardRetentionRepository.cs`：分表保留期仓储实现，在危险动作隔离器保护下执行分表删除并输出审计日志，且可基于系统元数据生成可回放回滚 DDL。
- `SyncCheckpointRepository.cs`：检查点文件持久化实现，读写日志均以 Information 级落盘；写入改为临时文件 + File.Replace/Move 原子替换，防止崩溃产生半写 JSON。
- `SyncBatchRepository.cs`：同步批次仓储 SQL Server 持久化分片实现，写入 `sync_batches_{yyyyMM}`，支持跨分片查询最近失败批次。
- `SyncBatchEntity.cs` + `SyncBatchEntityTypeConfiguration.cs`：同步批次实体与映射配置，定义批次状态流转字段、唯一约束与查询索引。
- `SyncChangeLogRepository.cs`：同步变更日志仓储 SQL Server 持久化分片实现，按 `ChangedTimeLocal` 写入 `sync_change_logs_{yyyyMM}`，并已替换及移除原内存实现。
- `SyncDeletionLogRepository.cs`：同步删除日志仓储 SQL Server 持久化分片实现，按 `DeletedTimeLocal`（为空时按入库时间）写入 `sync_deletion_logs_{yyyyMM}`，并已替换及移除原内存实现。
- `ServiceCollectionExtensions.cs`：统一注册基础设施依赖，并在启动阶段从启用同步表配置提取逻辑表名集合，完成安全校验与空配置异常拦截。
- `20260408020833_RebuildInitialHubSchema.cs`：初始化迁移，定义 `sorting_task_trace`、`IDX_PICKTOLIGHT_CARTON1`、`IDX_PICKTOWCS2` 三张聚合表结构及索引。
- `20260413144042_AddBusinessTaskTable.cs`：新增 `business_tasks` 迁移基线，作为分片模板来源，包含任务编码、条码、格口、扫描落格时间、状态、回传状态等字段及唯一索引。
- `20260413160852_AddScanDropLogTables.cs`：新增 `scan_logs` 与 `drop_logs` 迁移基线，作为分片模板来源，包含审计字段与查询索引。
- `20260416010041_AddSyncBatchShardTable.cs`：新增 `sync_batches` 基础表迁移，用于同步批次自动迁移基线与分片模板。
- `20260416171508_AddSyncChangeDeletionLogShardTables.cs`：新增 `sync_change_logs` 与 `sync_deletion_logs` 基础表迁移，用于同步变更/删除日志自动迁移基线与分片模板。
- `Properties/AssemblyInfo.cs`：为基础设施程序集声明 `InternalsVisibleTo("EverydayChain.Hub.Tests")`，支持测试项目直接验证 internal 成员。
- `nlog.config`：NLog 日志配置，输出至控制台与两个滚动日志文件：通用日志（`hub-${shortdate}.log`，按日切割，单文件上限 10 MB，保留 30 天）；同步专属日志（`sync-${shortdate}.log`，仅收录同步链路相关组件日志，便于独立分析同步性能问题）。
- `Program.cs`（Host）：Host 启动入口，现已支持 API + Worker 共存，启用 Controllers、Swagger（中文注释）并保留自动迁移与同步后台任务注册。
- `Host/Controllers/ScanController.cs` / `ChuteController.cs` / `DropFeedbackController.cs` / `WaveCleanupController.cs`：对外 API 控制器，仅做入参校验、调用应用服务与统一响应封装；`WaveCleanupController` 提供波次清理 dry-run 与正式执行端点。
- `Host/Contracts/Requests/*.cs` + `Host/Contracts/Responses/*.cs`：API 输入输出契约与统一响应包装，配合 Swagger 提供中文参数说明；其中 `WaveCleanupRequest`/`WaveCleanupResponse` 用于波次清理接口。
- `SyncBackgroundWorker.cs`：同步后台任务，按 `SyncJob.PollingIntervalSeconds` 周期触发全部启用表同步；支持表级超时保护（`TableSyncTimeoutSeconds`）；内置看门狗卡死检测（`WatchdogTimeoutSeconds`，主循环超过阈值未推进时输出 Critical 日志）；每轮输出整体汇总指标日志（总表数、失败表数、整体失败率、最大滞后/积压、轮次耗时）。
- `RetentionBackgroundWorker.cs`：保留期后台任务，按 `RetentionJob.PollingIntervalSeconds` 周期触发分表保留期治理。
- `FeedbackCompensationBackgroundWorker.cs`：业务回传补偿后台任务，按 `FeedbackCompensationJob.PollingIntervalSeconds` 周期重试失败回传任务，支持批次上限控制并输出补偿统计日志。
- `EverydayChain.Hub.Tests/Host/Controllers/*Tests.cs`：PR-03 新增 Controller 基础行为测试，覆盖空参校验与标准成功响应路径。
- `EverydayChain.Hub.Tests/Services/DangerZoneExecutorTests.cs`：危险操作隔离器取消语义测试，覆盖调用方取消与非调用方取消的日志等级分支。
- `EverydayChain.Hub.Tests/Services/TestLogger.cs`：通用测试日志记录器，集中承载日志采集替身，避免在测试文件内重复声明嵌套日志类型。
- `EverydayChain.Hub.Tests/Services/LoggerNullScope.cs`：测试日志空作用域单例，供测试日志记录器复用，避免重复创建无状态作用域实例。
- `EverydayChain.Hub.Tests/Services/SyncWindowCalculatorTests.cs`：SyncWindowCalculator 时间窗口回归测试套件（12 个测试用例，覆盖正常窗口、时钟回拨冻结、UTC 拒绝、Unspecified Kind 兼容、时钟扰动组合场景）。
- `EverydayChain.Hub.Tests/Services/AutoMigrationServiceTests.cs`：分表预建后缀策略测试，断言启动预建不再包含无后缀基础表。
- `EverydayChain.Hub.Tests/Services/FixedBootstrapShardSuffixResolver.cs`：分表后缀解析器测试替身，固定返回可控启动后缀集合用于自动迁移后缀策略测试。
- `EverydayChain.Hub.Tests/Services/ServiceCollectionExtensionsTests.cs`：逻辑表名构建测试，覆盖非法标识符与空启用集合异常场景。
- `EverydayChain.Hub.Tests/Services/BusinessTaskMaterializerTests.cs`：业务任务物化服务测试，覆盖默认状态赋值、时间赋值与必填字段空白校验分支。
- `EverydayChain.Hub.Tests/Services/BarcodeParserTests.cs`：条码解析服务测试，覆盖拆零、整件、不支持条码三类解析分支。
- `EverydayChain.Hub.Tests/Services/ScanIngressServiceTests.cs`：扫描上传应用服务测试，覆盖无效条码失败语义、无匹配任务返回未命中、有效任务受理分支；含内存仓储替身 `InMemoryBusinessTaskRepository`。
- `EverydayChain.Hub.Tests/Services/ScanMatchServiceTests.cs`：扫描匹配服务测试，覆盖空条码拒绝、无任务未命中、有任务匹配成功分支。
- `EverydayChain.Hub.Tests/Services/TaskExecutionServiceTests.cs`：任务执行服务测试，覆盖无任务失败、已创建任务推进、非法状态拒绝、持久化验证与扫描维度字段写入场景。
- `EverydayChain.Hub.Tests/Services/ChuteQueryServiceTests.cs`：请求格口服务测试，覆盖任务不存在、状态非法、无目标格口、成功解析、任务编码优先五个场景。
- `EverydayChain.Hub.Tests/Services/DropFeedbackServiceTests.cs`：落格回传服务测试，覆盖双空参数失败、任务不存在、条码冲突、状态非法、成功落格→Dropped、失败落格→Exception 六个场景。
- `EverydayChain.Hub.Tests/Services/SortingTaskTraceWriterTests.cs`：分表写入器兜底建表测试，覆盖首次写入先建表与同月重复写入幂等建表触发场景。
- `EverydayChain.Hub.Tests/Services/LocalDateTimeNormalizerTests.cs`：本地时间规范化工具测试，覆盖 UTC 拒绝、`Unspecified` 转本地与 `MinValue` 回退本地当前时间分支。
- `EverydayChain.Hub.Tests/Services/RecordingShardTableProvisioner.cs`：分表预建器测试替身，记录触发后缀以验证建表调用次数与后缀分发行为。
- `EverydayChain.Hub.Tests/Services/PassThroughSqlExecutionTuner.cs`：SQL 调谐器测试替身，提供恒定批大小用于隔离写入器行为测试。
- `EverydayChain.Hub.Tests/Services/ThrowingHubDbContextFactory.cs`：DbContext 工厂测试替身，强制抛错用于验证“先建表后建上下文”调用顺序。
- `EverydayChain.Hub.Tests/Services/ShardTableProvisionerTests.cs`：分表模板回归测试，覆盖并发上限钳制、空纳管拦截、实体模型到 DDL 的类型/主键/索引映射断言。
- `EverydayChain.Hub.Tests/Services/HubDbContextTestFactory.cs`：HubDbContext 测试工厂，集中承载上下文构造逻辑，避免测试文件内多类定义。
- `EverydayChain.Hub.Tests/Repositories/OracleSourceReaderTests.cs`：Oracle 连接串构建测试，覆盖空连接串、空库名、EZCONNECT（斜杠/SID）覆写与复杂描述符拦截分支。
- `EverydayChain.Hub.Tests/Repositories/SyncStagingRepositoryTests.cs`：暂存仓储回归测试，覆盖暂存行字段大小写不敏感访问，防止业务键字段因列名大小写差异导致读取失败。
- `EverydayChain.Hub.Tests/Repositories/SqlServerSyncUpsertRepositoryTests.cs`：SQL Server 落库仓储契约测试，覆盖插入/更新/跳过统计、UniqueKeys 缺失异常，以及 `sync_target_state` 状态分表命名安全边界（正常路径×3 + TableCode 非法字符 + 月份标记非法）。
- `EverydayChain.Hub.Tests/Repositories/SyncTaskConfigRepositoryTests.cs`：配置映射测试，覆盖 `SyncMode` 默认值与 `StatusDriven + PendingStatusValue=null` 映射语义。
- `EverydayChain.Hub.Tests/Sync/RemoteStatusConsumeServiceTests.cs` + `EverydayChain.Hub.Tests/Sync/Fakes/*.cs`：状态驱动消费测试与替身，覆盖追加、回写、缺失 `__RowId` 跳过回写统计路径。
- `EverydayChain.Hub.Tests/Repositories/InMemorySqlServerSyncUpsertRepository.cs`：SqlServerSyncUpsertRepository 内存测试替身，集中维护状态与分片迁移断言逻辑。
- `EverydayChain.Hub.Tests/Repositories/NoOpShardTableProvisioner.cs`：空实现分表预置器测试替身，用于隔离仓储合并测试的建表外部依赖。
- `EFCore手动迁移操作指南.md`：提供手工迁移、脚本导出、回滚、排障流程。
- `持续运行一年稳定性改造清单.md`：面向"连续运行一年"目标的稳定性改造清单，按 P0/P1/P2 组织改造优先级、待确认项与验收标准。
- `年度维护清单.md`：月度/季度/年度例行巡检清单，覆盖磁盘、日志、数据一致性、配置审核、依赖升级、灾难恢复与安全审计。
- `当前程序能力与缺陷分析.md`：汇总当前程序能力、功能清单、代码缺陷与逻辑 BUG，作为后续修复与优化输入。
- `Oracle到SQLServer同步架构设计.md`：定义外部 Oracle DB First 只读同步到本地 SQL Server 的详细落地方案。
- `Oracle到SQLServer同步实施计划.md`：按 PR 拆分同步架构落地步骤的进度跟踪文档。
- `EverydayChain.Hub_详细业务背景开发指令_v2.md`：项目详细业务背景与开发指令参考文档 v2，记录业务领域知识与 Copilot 开发约定。
- `EverydayChain.Hub_详细业务背景开发指令_v2_实施计划.md`：详细业务背景指令 v2 的实施总计划，定义多 PR 阶段目标、依赖顺序、验收门禁、风险拦截与待确认项，确保跨阶段执行保持一致目标。
- `兼容现有实现的可切换同步模式改造分析与执行步骤.md`：基于现有代码的双模式改造分析文档，明确保留链路、StatusDriven 停用项、分层新增文件与实施步骤。
- `ShardingOptions.cs`：分表配置模型，仅保留连接、Schema 与自动预建月数等基础配置。
- `AutoTuneOptions.cs`：批量写入自动调谐配置，从 `AutoTune` 节点绑定，覆盖初始/最小/最大批量、步长、慢阈值与采样窗口等参数。
- `DangerZoneOptions.cs`（Domain/Options）：危险操作隔离器弹性策略配置，从 `DangerZone` 节点绑定，覆盖超时、重试与熔断参数。
- `SyncTableOptions.cs`：单表同步配置，承载 `TableCode`、`SourceSchema`、`SourceTable`、`CursorColumn`、`StartTimeLocal`、`SyncMode`、状态驱动参数与删除/保留期子配置。
- `SyncDeleteOptions.cs`：单表删除同步配置子模型，承载 `DeletionPolicy`、`Enabled`、`DryRun`、比对分段大小与并行度。
- `SyncRetentionOptions.cs`：单表保留期治理配置子模型，承载 `Enabled`、`KeepMonths`、`DryRun` 与 `AllowDrop` 开关。
- `ShardTableProvisioner.cs`：分表预建实现，按启用同步表推导的逻辑表与后缀笛卡尔组合执行建表，并保持危险动作隔离执行。
- `AutoMigrationService.cs`：应用启动迁移入口，自动创建缺失数据库、识别并执行待迁移项，通过分表预建器自动覆盖多逻辑表。
- `appsettings.json`：主配置样例，移除分表逻辑表名静态配置，统一由 `SyncJob.Tables.TargetLogicalTable` 提供。
- `install.bat`：Windows 服务安装脚本，将 Host 程序注册为 Windows Service，配置开机自启、失败自动恢复策略（5 秒×3 次，含非崩溃退出），需以管理员身份运行。
- `uninstall.bat`：Windows 服务卸载脚本，停止并删除已注册的 Windows Service，需以管理员身份运行。
- `Properties/launchSettings.json`：本地开发启动配置，设定 `DOTNET_ENVIRONMENT=Development` 环境变量，供 `dotnet run` 与 IDE 调试使用。

### Oracle 配置速查（针对 `SELECT * FROM WMS_USER_431.IDX_PICKTOLIGHT_CARTON1`）
- `SyncJob.Tables[*].SourceSchema = WMS_USER_431`（源端 Schema 改为逐表必填）。
- `Oracle.Database = <真实 ServiceName 或 SID>`（例如 `ORCL`，以 DBA 提供为准）。
- `Oracle.DatabaseMode`：
  - 已知 `Database` 是 ServiceName：`ServiceName`
  - 已知 `Database` 是 SID：`Sid`
  - 不确定：`Auto`（默认按 ServiceName 语义拼接）。
- `Oracle.ConnectionString` 保持 `Data Source=host:port;User Id=...;Password=...;`（由系统按 `DatabaseMode` + `Database` 拼接库名）。

## 可继续完善内容（本次 PR 后续行动项）
- 为集合式 MERGE 增加真实 SQL Server 集成基准（大页写入、锁等待、超时重试）并沉淀基线阈值告警。
- 评估 TVP 版本的集合式 MERGE 实现，减少临时表 DDL 与批次内元数据开销。
- 增加“取消触发下的数据库事务回滚”集成测试，补齐真实数据库回滚行为验收。
- 为不同业务表列集差异场景补充列签名分组覆盖测试，进一步降低回归风险。
- 将“逐文件代码检查方案”沉淀为可复用的检查台账模板（含自动统计未检查文件能力）。
