# EverydayChain.Hub

## 本次更新内容
- 通读现有仓库代码并对照 `EverydayChain.Hub_详细业务背景开发指令_v2_实施计划.md` 完成执行进度盘点，明确当前从 PR-01 开始实施。
- 新增 PR-01 业务任务主模型：`BusinessTaskStatus` 与 `BusinessTaskEntity`。
- 新增 PR-01 物化服务：`IBusinessTaskMaterializer`、`BusinessTaskMaterializer`、`BusinessTaskMaterializeRequest`，并完成 DI 注册。
- 新增 `BusinessTaskMaterializer` 单元测试，覆盖默认状态与必填校验。
- 更新 `EverydayChain.Hub_详细业务背景开发指令_v2_实施计划.md`：补全 PR 总览“当前状态”与“待确认项”，并登记 PR-01 已完成。
- 构建验证：0 Warning 0 Error，70/70 单元测试通过。
## 后续可完善点
- 按待确认项先固化 PR-02 文档目录与命名规范，再推进接口基线文档拆分。
- 在 PR-03 前冻结三类 API 的认证与幂等键来源，避免接口返工。
- 在 PR-05 前确认业务任务持久化方案（新表或复用现有聚合映射），并提前评审迁移脚本边界。


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
├── scripts
│   ├── health-check.sh
│   ├── disaster-recovery.sh
│   └── stability-drill.sh
├── .github
│   ├── copilot-instructions.md
│   ├── DDD分层接口与实现放置规范.md
│   └── workflows
│       └── copilot-governance.yml
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
│   ├── Aggregates/WmsPickToWcsAggregate/WmsPickToWcsEntity.cs
│   ├── Aggregates/WmsSplitPickToLightCartonAggregate/WmsSplitPickToLightCartonEntity.cs
│   ├── Options/AutoTuneOptions.cs
│   ├── Options/DangerZoneOptions.cs
│   ├── Options/OracleOptions.cs
│   ├── Options/RetentionJobOptions.cs
│   ├── Options/ShardingOptions.cs
│   ├── Options/SyncDeleteOptions.cs
│   ├── Options/SyncJobOptions.cs
│   ├── Options/SyncRetentionOptions.cs
│   └── Options/SyncTableOptions.cs
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
│   ├── Models/RemoteStatusConsumeResult.cs
│   ├── Services/SyncOrchestrator.cs
│   ├── Services/SyncWindowCalculator.cs
│   ├── Services/SyncExecutionService.cs
│   ├── Services/BusinessTaskMaterializer.cs
│   ├── Services/DeletionExecutionService.cs
│   └── Services/RetentionExecutionService.cs
├── EverydayChain.Hub.SharedKernel
│   ├── EverydayChain.Hub.SharedKernel.csproj
│   └── Utilities
│       ├── LogicalTableNameNormalizer.cs
│       ├── RuntimeStoragePathResolver.cs
│       ├── BoundedConcurrentQueueHelper.cs
│       ├── SyncBusinessKeyBuilder.cs
│       └── SyncColumnFilter.cs
├── EverydayChain.Hub.Infrastructure
│   ├── EverydayChain.Hub.Infrastructure.csproj
│   ├── DependencyInjection/ServiceCollectionExtensions.cs
│   ├── Properties/AssemblyInfo.cs
│   ├── Sync/Readers/OracleStatusDrivenSourceReader.cs
│   ├── Sync/Writers/SqlServerAppendOnlyWriter.cs
│   ├── Sync/Writers/OracleRemoteStatusWriter.cs
│   ├── Sync/Services/RemoteStatusConsumeService.cs
│   ├── Repositories/SyncTaskConfigRepository.cs
│   ├── Repositories/OracleSourceReader.cs
│   ├── Repositories/SyncStagingRepository.cs
│   ├── Repositories/SqlServerSyncUpsertRepository.cs
│   ├── Repositories/SyncDeletionRepository.cs
│   ├── Repositories/ShardTableResolver.cs
│   ├── Repositories/ShardRetentionRepository.cs
│   ├── Repositories/SyncCheckpointRepository.cs
│   ├── Repositories/InMemorySyncBatchRepository.cs
│   ├── Repositories/InMemorySyncChangeLogRepository.cs
│   ├── Repositories/InMemorySyncDeletionLogRepository.cs
│   ├── Persistence/HubDbContext.cs
│   ├── Persistence/DesignTimeHubDbContextFactory.cs
│   ├── Persistence/EntityConfigurations/SortingTaskTraceEntityTypeConfiguration.cs
│   ├── Persistence/Sharding/TableSuffixScope.cs
│   ├── Persistence/Sharding/IShardSuffixResolver.cs
│   ├── Persistence/Sharding/MonthShardSuffixResolver.cs
│   ├── Persistence/Sharding/ShardModelCacheKeyFactory.cs
│   ├── Migrations/20260408020833_RebuildInitialHubSchema.cs
│   ├── Migrations/20260408020833_RebuildInitialHubSchema.Designer.cs
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
│       ├── ShardTableProvisionerTests.cs
│       ├── SortingTaskTraceWriterTests.cs
│       ├── TestLogger.cs
│       ├── ThrowingHubDbContextFactory.cs
│       └── SyncWindowCalculatorTests.cs
└── EverydayChain.Hub.Host
    ├── EverydayChain.Hub.Host.csproj
    ├── Program.cs
    ├── Workers/SyncBackgroundWorker.cs
    ├── Workers/RetentionBackgroundWorker.cs
    ├── Workers/AutoMigrationHostedService.cs
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
- `scripts/health-check.sh`：一键体检脚本，检查磁盘空间、目录权限、关键文件可读写、配置文件格式、日志健康状态、进程存活与压缩归档文件状态，可集成到监控或定时任务。
- `scripts/disaster-recovery.sh`：灾难恢复脚本，支持检查点重置（checkpoint-reset）、快照从归档恢复（snapshot-restore）、快照备份（snapshot-backup）、归档清理（archive-cleanup）与完全重置（full-reset）；全部操作支持 --dry-run 预览模式。
- `scripts/stability-drill.sh`：稳定性演练脚本，串联体检与灾备动作（checkpoint-reset、snapshot-backup、snapshot-restore、archive-cleanup），支持 dry-run 与真实执行并自动生成演练记录。
- `监控告警规则基线清单.md`：监控告警规则基线文档，定义日志关键字告警、指标阈值告警与演练留档验收口径，用于补齐稳定性清单剩余交付项。
- `年度维护清单.md`：月度/季度/年度例行巡检项标准化清单，包含磁盘治理、日志审查、数据一致性、配置审核、灾难恢复演练、容量规划、安全审计等条目及快速异常处理参考表。
- `值班处置手册.md`：日常值班与告警应急处置手册，覆盖 9 类告警的处置步骤（卡死检测、磁盘不足、内存水位、整轮超时、熔断、检查点损坏、快照损坏、归档失败、进程停止），定义 P0~P3 优先级与升级规则，含处置记录与演练记录模板。
- `逐文件代码检查方案.md`：逐文件审查执行方案，定义检查范围、单文件检查维度、无遗漏对账流程、问题分级与分批 PR 策略，支持“本 PR 不改代码”的审查场景。
- `逐文件全量审查实施方案.md`：续审执行方案，要求先核对首轮已处理内容，再对首轮台账未覆盖文件执行补审并闭环。
- `逐文件代码检查台账.md`：逐文件检查台账（首轮 155 文件 + 续审批次 A 补齐 15 文件），记录每文件检查状态（未检查/进行中/已完成）、问题编号与修复状态，供后续 PR 复核追溯。
- `SyncTableDefinition.cs` / `SyncWindow.cs` / `SyncCheckpoint.cs` / `SyncBatchResult.cs`：定义同步链路执行、窗口与结果统计的核心领域模型。
- `SyncBatch.cs` / `SyncChangeLog.cs` / `SyncDeletionLog.cs`：定义批次状态跟踪、变更审计与删除审计的数据模型。
- `SyncBusinessKeyBuilder.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：同步业务键构建共享组件，按 `UniqueKeys` 配置将行数据拼接为 `|` 分隔的业务键文本，供 Upsert 与删除识别阶段统一调用。
- `SyncColumnFilter.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：同步列过滤共享组件，提供 `ExcludedColumns` 规范化与行级过滤能力，并统一维护软删除关键列常量。
- `RuntimeStoragePathResolver.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：运行期路径解析共享组件，统一解析检查点、目标快照与存储守护所需的绝对路径。
- `LogicalTableNameNormalizer.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：逻辑表名规范化与安全校验共享组件，统一执行去空白、SQL 标识符校验与异常信息输出。
- `BoundedConcurrentQueueHelper.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：有界并发队列淘汰辅助工具，仅执行一次 O(n) `Count` 遍历并缓存结果，供需要内存容量保护的队列实现统一复用。
- `SyncMode.cs` / `DeletionPolicy.cs` / `LagControlMode.cs` / `SyncBatchStatus.cs` / `SyncChangeOperationType.cs` / `SyncTablePriority.cs`：同步模式、删除策略、滞后控制、批次状态、变更操作类型与调度优先级枚举，均含中文 XML 注释与 `Description`。
- `BusinessTaskStatus.cs`：业务任务生命周期状态枚举，覆盖 Created、Scanned、Dropped、FeedbackPending，并提供中文 `Description` 说明。
- `RemoteStatusConsumeProfile.cs`（`EverydayChain.Hub.Domain/Sync/Models`）：StatusDriven 消费配置模型，统一承载状态列、待处理值、完成值、回写开关与批次大小。
- `EverydayChain.Hub.Domain/Options/*.cs`：统一承载全部配置实体（`Sharding`、`AutoTune`、`DangerZone`、`SyncJob`、`SyncTable`、`SyncDelete`、`SyncRetention`、`RetentionJob`、`Oracle` 等），供 Infrastructure 绑定读取。
- `SortingTaskTraceEntity.cs`：可分表的写入实体，承载中台追踪数据；所有属性均含 XML 注释。
- `BusinessTaskEntity.cs`（`Domain/Aggregates/BusinessTaskAggregate`）：统一业务任务聚合根实体，承载任务编码、来源表、业务键、条码与本地状态时间字段。
- `SyncExecutionContext.cs` + `SyncReadRequest.cs` + `SyncReadResult.cs` + `SyncMergeRequest.cs` + `SyncMergeResult.cs` + `SyncDeletionDetectRequest.cs` + `SyncDeletionApplyRequest.cs` + `SyncDeletionExecutionResult.cs` + `SyncDeletionCandidate.cs` + `SyncKeyReadRequest.cs` + `SyncTargetStateRow.cs`：同步执行、删除识别与轻量幂等状态存储的数据契约模型。
- `BusinessTaskMaterializeRequest.cs`：业务任务物化输入模型，统一约束任务编码、来源表编码、业务键、条码与物化时间字段。
- `Application/Abstractions/Services/IBusinessTaskMaterializer.cs` + `Application/Services/BusinessTaskMaterializer.cs`：业务任务物化服务抽象与实现，仅执行字段映射、文本规范化和默认状态赋值，不承载扫描/格口/落格业务规则。
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
- `InMemorySyncBatchRepository.cs`：同步批次仓储内存实现，支持 `Pending/InProgress/Completed/Failed` 状态流转与最近失败批次查询。
- `InMemorySyncChangeLogRepository.cs`：同步变更日志仓储内存实现，支持批量写入审计记录。
- `InMemorySyncDeletionLogRepository.cs`：同步删除日志仓储内存实现，支持批量写入删除审计记录（含 DryRun 执行标记）。
- `ServiceCollectionExtensions.cs`：统一注册基础设施依赖，并在启动阶段从启用同步表配置提取逻辑表名集合，完成安全校验与空配置异常拦截。
- `20260408020833_RebuildInitialHubSchema.cs`：初始化迁移，定义 `sorting_task_trace`、`IDX_PICKTOLIGHT_CARTON1`、`IDX_PICKTOWCS2` 三张聚合表结构及索引。
- `Properties/AssemblyInfo.cs`：为基础设施程序集声明 `InternalsVisibleTo("EverydayChain.Hub.Tests")`，支持测试项目直接验证 internal 成员。
- `nlog.config`：NLog 日志配置，输出至控制台与两个滚动日志文件：通用日志（`hub-${shortdate}.log`，按日切割，单文件上限 10 MB，保留 30 天）；同步专属日志（`sync-${shortdate}.log`，仅收录同步链路相关组件日志，便于独立分析同步性能问题）。
- `SyncBackgroundWorker.cs`：同步后台任务，按 `SyncJob.PollingIntervalSeconds` 周期触发全部启用表同步；支持表级超时保护（`TableSyncTimeoutSeconds`）；内置看门狗卡死检测（`WatchdogTimeoutSeconds`，主循环超过阈值未推进时输出 Critical 日志）；每轮输出整体汇总指标日志（总表数、失败表数、整体失败率、最大滞后/积压、轮次耗时）。
- `RetentionBackgroundWorker.cs`：保留期后台任务，按 `RetentionJob.PollingIntervalSeconds` 周期触发分表保留期治理。
- `EverydayChain.Hub.Tests/Services/DangerZoneExecutorTests.cs`：危险操作隔离器取消语义测试，覆盖调用方取消与非调用方取消的日志等级分支。
- `EverydayChain.Hub.Tests/Services/TestLogger.cs`：通用测试日志记录器，集中承载日志采集替身，避免在测试文件内重复声明嵌套日志类型。
- `EverydayChain.Hub.Tests/Services/LoggerNullScope.cs`：测试日志空作用域单例，供测试日志记录器复用，避免重复创建无状态作用域实例。
- `EverydayChain.Hub.Tests/Services/SyncWindowCalculatorTests.cs`：SyncWindowCalculator 时间窗口回归测试套件（12 个测试用例，覆盖正常窗口、时钟回拨冻结、UTC 拒绝、Unspecified Kind 兼容、时钟扰动组合场景）。
- `EverydayChain.Hub.Tests/Services/AutoMigrationServiceTests.cs`：分表预建后缀策略测试，断言启动预建不再包含无后缀基础表。
- `EverydayChain.Hub.Tests/Services/FixedBootstrapShardSuffixResolver.cs`：分表后缀解析器测试替身，固定返回可控启动后缀集合用于自动迁移后缀策略测试。
- `EverydayChain.Hub.Tests/Services/ServiceCollectionExtensionsTests.cs`：逻辑表名构建测试，覆盖非法标识符与空启用集合异常场景。
- `EverydayChain.Hub.Tests/Services/BusinessTaskMaterializerTests.cs`：业务任务物化服务测试，覆盖默认状态赋值、时间赋值与必填字段空白校验分支。
- `EverydayChain.Hub.Tests/Services/SortingTaskTraceWriterTests.cs`：分表写入器兜底建表测试，覆盖首次写入先建表与同月重复写入幂等建表触发场景。
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
