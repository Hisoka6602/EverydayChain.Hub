# EverydayChain.Hub

## 本次更新内容
- 继续实施《Oracle到SQLServer同步实施计划.md》PR-2：新增批次状态枚举 `SyncBatchStatus`，以及 `SyncBatch`、`SyncChangeLog` 领域模型。
- 新增批次与变更日志仓储契约：`ISyncBatchRepository`、`ISyncChangeLogRepository`，并提供基础设施内存实现 `SyncBatchRepository`、`SyncChangeLogRepository`。
- 改造同步执行链路：新增批次状态流转（`Pending -> InProgress -> Completed/Failed`）与 `ParentBatchId` 重试关联，并在“读取+合并+日志写入”成功后再提交检查点。
- 更新实施计划：移除已完全落地的 PR-1 条目与 PR-2 主体条目，保留 `OperationType` 细分待办。

## 解决方案文件树与职责
```text
.
├── EverydayChain.Hub.sln
├── README.md
├── EFCore手动迁移操作指南.md
├── Oracle到SQLServer同步架构设计.md
├── Oracle到SQLServer同步实施计划.md
├── .github
│   ├── copilot-instructions.md
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
│   ├── Sync/SyncTableDefinition.cs
│   ├── Sync/SyncWindow.cs
│   ├── Sync/SyncCheckpoint.cs
│   ├── Sync/SyncBatchResult.cs
│   ├── Sync/SyncBatch.cs
│   ├── Sync/SyncChangeLog.cs
│   ├── Aggregates/SortingTaskTraceAggregate/SortingTaskTraceEntity.cs
│   ├── Aggregates/WmsPickToWcsAggregate/WmsPickToWcsEntity.cs
│   └── Aggregates/WmsSplitPickToLightCartonAggregate/WmsSplitPickToLightCartonEntity.cs
├── EverydayChain.Hub.Application
│   ├── EverydayChain.Hub.Application.csproj
│   ├── Models/SyncExecutionContext.cs
│   ├── Models/SyncReadRequest.cs
│   ├── Models/SyncReadResult.cs
│   ├── Models/SyncMergeRequest.cs
│   ├── Models/SyncMergeResult.cs
│   ├── Repositories/ISyncTaskConfigRepository.cs
│   ├── Repositories/IOracleSourceReader.cs
│   ├── Repositories/ISyncStagingRepository.cs
│   ├── Repositories/ISyncUpsertRepository.cs
│   ├── Repositories/ISyncCheckpointRepository.cs
│   ├── Repositories/ISyncBatchRepository.cs
│   ├── Repositories/ISyncChangeLogRepository.cs
│   ├── Services/ISyncOrchestrator.cs
│   ├── Services/ISyncWindowCalculator.cs
│   ├── Services/ISyncExecutionService.cs
│   ├── Services/SyncOrchestrator.cs
│   ├── Services/SyncWindowCalculator.cs
│   └── Services/SyncExecutionService.cs
├── EverydayChain.Hub.SharedKernel
│   ├── EverydayChain.Hub.SharedKernel.csproj
│   └── Class1.cs
├── EverydayChain.Hub.Infrastructure
│   ├── EverydayChain.Hub.Infrastructure.csproj
│   ├── DependencyInjection/ServiceCollectionExtensions.cs
│   ├── Options/ShardingOptions.cs
│   ├── Options/AutoTuneOptions.cs
│   ├── Options/DangerZoneOptions.cs
│   ├── Options/SyncJobOptions.cs
│   ├── Options/SyncTableOptions.cs
│   ├── Repositories/SyncTaskConfigRepository.cs
│   ├── Repositories/OracleSourceReader.cs
│   ├── Repositories/SyncStagingRepository.cs
│   ├── Repositories/SyncUpsertRepository.cs
│   ├── Repositories/SyncCheckpointRepository.cs
│   ├── Repositories/SyncBatchRepository.cs
│   ├── Repositories/SyncChangeLogRepository.cs
│   ├── Persistence/HubDbContext.cs
│   ├── Persistence/DesignTimeHubDbContextFactory.cs
│   ├── Persistence/EntityConfigurations/SortingTaskTraceEntityTypeConfiguration.cs
│   ├── Persistence/Sharding/TableSuffixScope.cs
│   ├── Persistence/Sharding/IShardSuffixResolver.cs
│   ├── Persistence/Sharding/MonthShardSuffixResolver.cs
│   ├── Persistence/Sharding/ShardModelCacheKeyFactory.cs
│   ├── Migrations/202603280001_InitialHubSchema.cs
│   ├── Migrations/HubDbContextModelSnapshot.cs
│   └── Services
│       ├── IDangerZoneExecutor.cs
│       ├── DangerZoneExecutor.cs
│       ├── IAutoMigrationService.cs
│       ├── AutoMigrationService.cs
│       ├── AutoMigrationHostedService.cs
│       ├── IShardTableProvisioner.cs
│       ├── ShardTableProvisioner.cs
│       ├── ISqlExecutionTuner.cs
│       ├── SqlExecutionTuner.cs
│       ├── ISortingTaskTraceWriter.cs
│       └── SortingTaskTraceWriter.cs
└── EverydayChain.Hub.Host
    ├── EverydayChain.Hub.Host.csproj
    ├── Program.cs
    ├── Worker.cs
    ├── Workers/SyncBackgroundWorker.cs
    ├── nlog.config
    ├── appsettings.json
    └── Options/WorkerOptions.cs
```

## 各层级与各文件作用说明（逐项）
- `.github/copilot-instructions.md`：定义仓库级 Copilot 强制约束，覆盖时间语义、结构规范、文档联动与交付门禁。
- `.github/workflows/copilot-governance.yml`：执行规则自动校验，并强制规则文件与工作流联动修改。
- `SyncTableDefinition.cs` / `SyncWindow.cs` / `SyncCheckpoint.cs` / `SyncBatchResult.cs`：定义同步链路执行、窗口与结果统计的核心领域模型。
- `SyncBatch.cs` / `SyncChangeLog.cs`：定义批次状态跟踪与变更审计的数据模型。
- `SyncMode.cs` / `DeletionPolicy.cs` / `LagControlMode.cs` / `SyncBatchStatus.cs` / `SyncChangeOperationType.cs`：同步模式、删除策略、滞后控制、批次状态与变更操作类型枚举，均含中文 XML 注释与 `Description`。
- `SortingTaskTraceEntity.cs`：可分表的写入实体，承载中台追踪数据；所有属性均含 XML 注释。
- `SyncExecutionContext.cs` + `SyncReadRequest.cs` + `SyncReadResult.cs` + `SyncMergeRequest.cs` + `SyncMergeResult.cs`：同步执行上下文、分页读取与幂等合并的数据契约模型。
- `ISyncBatchRepository.cs` / `ISyncChangeLogRepository.cs`：定义批次状态持久化与变更日志写入契约。
- `ISyncOrchestrator.cs` / `SyncOrchestrator.cs`：同步任务编排入口，负责读取配置、加载检查点、计算窗口并触发批次执行。
- `ISyncWindowCalculator.cs` / `SyncWindowCalculator.cs`：根据 `CursorColumn + StartTimeLocal` 与检查点计算本地增量窗口。
- `ISyncExecutionService.cs` / `SyncExecutionService.cs`：执行分页读取、暂存、幂等合并、变更日志写入、检查点提交，并维护批次状态流转；异常场景输出 NLog 错误日志。
- `HubDbContext.cs`：根据分表后缀动态映射表名。
- `TableSuffixScope.cs` + `ShardModelCacheKeyFactory.cs`：保证不同后缀下 EF Model 能正确缓存隔离。
- `MonthShardSuffixResolver.cs`：按月份生成分表后缀（如 `_202603`）。
- `IShardTableProvisioner.cs` + `ShardTableProvisioner.cs`：在 SQL Server 中按需创建分表与索引（不存在才建），替代原 `ShardTableManager` 命名。
- `AutoMigrationService.cs` + `AutoMigrationHostedService.cs`：应用启动时自动执行 `Migrate` 与分表预创建。
- `SqlExecutionTuner.cs`：基于失败率和耗时进行批量窗口升降调谐；采样窗口大小与失败率阈值均来自 `AutoTuneOptions`。
- `DangerZoneExecutor.cs`：危险路径统一走隔离器（超时/重试/熔断），弹性参数来自 `DangerZoneOptions`。
- `DangerZoneOptions.cs`：`DangerZoneExecutor` 弹性策略配置类，绑定 `DangerZone` 节点，覆盖超时、重试、熔断全部参数，所有属性含 XML 注释。
- `SortingTaskTraceWriter.cs`：按分表后缀分组写入，并将执行结果回传给调谐器。
- `SyncJobOptions.cs` / `SyncTableOptions.cs`：同步任务全局与单表配置绑定模型，统一约束本地时间配置、分页、滞后窗口与唯一键。
- `SyncTaskConfigRepository.cs`：从 `SyncJob` 配置节读取表定义，并校验 `StartTimeLocal` 禁止 `Z` 与 offset。
- `OracleSourceReader.cs`：源端分页读取器基础实现，当前用内存种子数据模拟按窗口和唯一键稳定排序读取。
- `SyncStagingRepository.cs`：暂存仓储基础实现，按 `BatchId + PageNo` 进行内存暂存。
- `SyncUpsertRepository.cs`：幂等合并基础实现，支持 `UniqueKeys` 下插入/覆盖更新/一致跳过。
- `SyncCheckpointRepository.cs`：检查点文件持久化实现，支持失败后续跑。
- `SyncBatchRepository.cs`：同步批次仓储基础实现，支持 `Pending/InProgress/Completed/Failed` 状态流转与最近失败批次查询。
- `SyncChangeLogRepository.cs`：同步变更日志仓储基础实现，支持批量写入审计记录。
- `ServiceCollectionExtensions.cs`：统一注册基础设施依赖。
- `202603280001_InitialHubSchema.cs`：基础表结构迁移。
- `nlog.config`：NLog 日志配置，输出至控制台与滚动日志文件（按日切割，保留 30 天）。
- `WorkerOptions.cs`：后台工作服务配置类，绑定 `Worker` 节点，覆盖轮询间隔（`PollingIntervalSeconds`），含 XML 注释。
- `SyncBackgroundWorker.cs`：同步后台任务，按 `SyncJob.PollingIntervalSeconds` 周期触发全部启用表同步。
- `EFCore手动迁移操作指南.md`：提供手工迁移、脚本导出、回滚、排障流程。
- `Oracle到SQLServer同步架构设计.md`：定义外部 Oracle DB First 只读同步到本地 SQL Server 的详细落地方案，包含接口与实现命名、配置模型、可配置排除列、幂等覆盖、删除同步、分表保留期治理与验收清单。
- `Oracle到SQLServer同步实施计划.md`：按 PR 拆分同步架构落地步骤（最多 6 个 PR）的进度跟踪文档，定义完成项删除前必须通读代码确认完全实现的维护规则，随实施进展动态更新。

## 可继续完善内容
- 将 PR-1 剩余项从“基础实现”推进到“数据库落地实现”，包括真实 Oracle 读取、SQL Server 暂存表与 MERGE 语句对接。
- 补齐 PR-2 收尾：将 `SyncChangeLog.OperationType` 从当前 `Upsert` 细分为 `Insert/Update/Delete`。
- 实现 PR-3 删除同步：源端存在性差异识别、`DeletionPolicy` 执行、`SyncDeletionLog` 与 `SyncChangeLog` 联动写入。
- 实现 PR-4 配置治理：`ExcludedColumns` 关键列冲突校验与高低优先级表差异化调度。
- 实现 PR-5/PR-6：保留期治理危险动作门禁、并行优化、重试熔断与可观测性指标收口。
