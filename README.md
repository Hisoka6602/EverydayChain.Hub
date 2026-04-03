# EverydayChain.Hub

## 本次更新内容
- 配置文件注释方式改为参考 Zeye.NarrowBeltSorter 的 JSON 注释风格（`//` 行注释），并在 CI 中按‘每个配置项上方必须有注释’进行自动校验。
- 新增并落地结构强制约束：配置实体统一迁移至 `EverydayChain.Hub.Domain/Options`；静态工具类 `SyncBusinessKeyBuilder` 与 `SyncColumnFilter` 迁移至 `EverydayChain.Hub.SharedKernel/Utilities`；删除 SharedKernel 占位类 `Class1.cs`。
- CI 新增结构扫描：枚举目录、配置实体目录、聚合根目录、事件目录、静态工具类目录约束自动校验。
- 删除 `ISyncUpsertRepository.BuildBusinessKey` 转发方法及其实现（违反"禁止仅做一层转发的方法"规则），调用方 `SyncDeletionRepository` 改为直接调用 `SyncBusinessKeyBuilder.Build`。
- 修复 `WmsPickToWcsEntity.cs` 和 `WmsSplitPickToLightCartonEntity.cs` 命名空间格式：从老式花括号块改为文件范围 namespace，并移除无用的 using 导入。
- 修复 `SyncCheckpointRepository` 热路径性能问题：`JsonSerializerOptions` 从每次 SaveAsync 内联创建改为静态只读字段，避免重复分配。
- 修复 `SyncCheckpointRepository` 日志级别：读写检查点的业务日志从 `LogDebug` 改为 `LogInformation`，确保所有业务日志均落盘（NLog 当前 minLevel 为 Info）。
- 修复 `SyncCheckpointRepository.SaveAsync` 写入安全：改为临时文件 + `File.Replace`/`File.Move` 原子替换，防止进程崩溃时产生半写 JSON；写入失败时自动清理临时文件并输出错误日志。
- 修复 `DeletionExecutionService` 重复去重逻辑：合并两次独立的业务键去重操作，先生成 `uniqueCandidates`，再从中派生 `businessKeys`，减少一次不必要的遍历。
- 已完成 Copilot 仓库执行规范约束检查：确认无 UTC 时间 API 使用、命名空间与目录一致、配置项含中文注释、日志均路由至文件 target、无 Obsolete 标注、无 Manager/Helper/Wrapper 等禁用类名、无跨 PR 历史变更日志累积。

## 解决方案文件树与职责
```text
.
├── EverydayChain.Hub.sln
├── README.md
├── EFCore手动迁移操作指南.md
├── 当前程序能力与缺陷分析.md
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
│   ├── Enums/SyncTablePriority.cs
│   ├── Sync/SyncTableDefinition.cs
│   ├── Sync/SyncWindow.cs
│   ├── Sync/SyncCheckpoint.cs
│   ├── Sync/SyncBatchResult.cs
│   ├── Sync/SyncBatch.cs
│   ├── Sync/SyncChangeLog.cs
│   ├── Sync/SyncDeletionLog.cs
│   ├── Aggregates/SortingTaskTraceAggregate/SortingTaskTraceEntity.cs
│   ├── Aggregates/WmsPickToWcsAggregate/WmsPickToWcsEntity.cs
│   ├── Aggregates/WmsSplitPickToLightCartonAggregate/WmsSplitPickToLightCartonEntity.cs
│   ├── Options/WorkerOptions.cs
│   └── Options/RetentionJobOptions.cs
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
│   ├── Repositories/ISyncTaskConfigRepository.cs
│   ├── Repositories/IOracleSourceReader.cs
│   ├── Repositories/ISyncStagingRepository.cs
│   ├── Repositories/ISyncUpsertRepository.cs
│   ├── Repositories/ISyncCheckpointRepository.cs
│   ├── Repositories/ISyncBatchRepository.cs
│   ├── Repositories/ISyncChangeLogRepository.cs
│   ├── Repositories/ISyncDeletionRepository.cs
│   ├── Repositories/ISyncDeletionLogRepository.cs
│   ├── Repositories/IShardTableResolver.cs
│   ├── Repositories/IShardRetentionRepository.cs
│   ├── Services/ISyncOrchestrator.cs
│   ├── Services/ISyncWindowCalculator.cs
│   ├── Services/ISyncExecutionService.cs
│   ├── Services/IDeletionExecutionService.cs
│   ├── Services/IRetentionExecutionService.cs
│   ├── Services/SyncOrchestrator.cs
│   ├── Services/SyncWindowCalculator.cs
│   ├── Services/SyncExecutionService.cs
│   ├── Services/DeletionExecutionService.cs
│   └── Services/RetentionExecutionService.cs
├── EverydayChain.Hub.SharedKernel
│   ├── EverydayChain.Hub.SharedKernel.csproj
│   └── Utilities
│       ├── SyncBusinessKeyBuilder.cs
│       └── SyncColumnFilter.cs
├── EverydayChain.Hub.Infrastructure
│   ├── EverydayChain.Hub.Infrastructure.csproj
│   ├── DependencyInjection/ServiceCollectionExtensions.cs
│   ├── Repositories/SyncTaskConfigRepository.cs
│   ├── Repositories/OracleSourceReader.cs
│   ├── Repositories/SyncStagingRepository.cs
│   ├── Repositories/SyncUpsertRepository.cs
│   ├── Repositories/SyncDeletionRepository.cs
│   ├── Repositories/ShardTableResolver.cs
│   ├── Repositories/ShardRetentionRepository.cs
│   ├── Repositories/SyncCheckpointRepository.cs
│   ├── Repositories/SyncBatchRepository.cs
│   ├── Repositories/SyncChangeLogRepository.cs
│   ├── Repositories/SyncDeletionLogRepository.cs
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
    ├── Workers/RetentionBackgroundWorker.cs
    ├── nlog.config
    └── appsettings.json
```

## 各层级与各文件作用说明（逐项）
- `.github/copilot-instructions.md`：定义仓库级 Copilot 强制约束，覆盖时间语义、结构规范、文档联动与交付门禁。
- `.github/workflows/copilot-governance.yml`：执行规则自动校验，并强制规则文件与工作流联动修改。
- `SyncTableDefinition.cs` / `SyncWindow.cs` / `SyncCheckpoint.cs` / `SyncBatchResult.cs`：定义同步链路执行、窗口与结果统计的核心领域模型。
- `SyncBatch.cs` / `SyncChangeLog.cs` / `SyncDeletionLog.cs`：定义批次状态跟踪、变更审计与删除审计的数据模型。
- `SyncBusinessKeyBuilder.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：同步业务键构建共享组件，按 `UniqueKeys` 配置将行数据拼接为 `|` 分隔的业务键文本，供 Upsert 与删除识别阶段统一调用。
- `SyncColumnFilter.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：同步列过滤共享组件，提供 `ExcludedColumns` 规范化与行级过滤能力，并统一维护软删除关键列常量。
- `SyncMode.cs` / `DeletionPolicy.cs` / `LagControlMode.cs` / `SyncBatchStatus.cs` / `SyncChangeOperationType.cs` / `SyncTablePriority.cs`：同步模式、删除策略、滞后控制、批次状态、变更操作类型与调度优先级枚举，均含中文 XML 注释与 `Description`。
- `EverydayChain.Hub.Domain/Options/*.cs`：统一承载全部配置实体（`Worker`、`Sharding`、`AutoTune`、`DangerZone`、`SyncJob`、`RetentionJob` 等），供 Host/Infrastructure 绑定读取。
- `SortingTaskTraceEntity.cs`：可分表的写入实体，承载中台追踪数据；所有属性均含 XML 注释。
- `SyncExecutionContext.cs` + `SyncReadRequest.cs` + `SyncReadResult.cs` + `SyncMergeRequest.cs` + `SyncMergeResult.cs` + `SyncDeletionDetectRequest.cs` + `SyncDeletionApplyRequest.cs` + `SyncDeletionExecutionResult.cs` + `SyncDeletionCandidate.cs` + `SyncKeyReadRequest.cs`：同步执行、删除识别与删除执行的数据契约模型。
- `ISyncBatchRepository.cs` / `ISyncChangeLogRepository.cs` / `ISyncDeletionRepository.cs` / `ISyncDeletionLogRepository.cs`：定义批次状态、变更日志、删除识别执行与删除日志写入契约。
- `IShardTableResolver.cs` / `IShardRetentionRepository.cs`：定义分表识别与分表清理执行契约（含分表完整回滚脚本生成）。
- `ISyncOrchestrator.cs` / `SyncOrchestrator.cs`：同步任务编排入口，负责读取配置、加载检查点、计算窗口，并基于优先级与并发上限执行多表同步。
- `ISyncWindowCalculator.cs` / `SyncWindowCalculator.cs`：根据 `CursorColumn + StartTimeLocal` 与检查点计算本地增量窗口。
- `IDeletionExecutionService.cs` / `DeletionExecutionService.cs`：执行删除识别、删除策略应用（含 DryRun）并生成删除审计与删除变更日志。
- `IRetentionExecutionService.cs` / `RetentionExecutionService.cs`：执行分表保留期治理，完成过期分表识别、完整回滚脚本生成、dry-run 审计、删除执行、失败隔离与汇总。
- `ISyncExecutionService.cs` / `SyncExecutionService.cs`：执行分页读取、暂存、幂等合并、删除同步、日志写入、检查点提交，并输出延迟/积压/吞吐/失败率指标日志；异常场景输出 NLog 错误日志。
- `HubDbContext.cs`：根据分表后缀动态映射表名。
- `TableSuffixScope.cs` + `ShardModelCacheKeyFactory.cs`：保证不同后缀下 EF Model 能正确缓存隔离。
- `MonthShardSuffixResolver.cs`：按月份生成分表后缀（如 `_202603`）。
- `IShardTableProvisioner.cs` + `ShardTableProvisioner.cs`：在 SQL Server 中按需创建分表与索引（不存在才建），替代原 `ShardTableManager` 命名。
- `AutoMigrationService.cs` + `AutoMigrationHostedService.cs`：应用启动时自动执行 `Migrate` 与分表预创建。
- `SqlExecutionTuner.cs`：基于失败率和耗时进行批量窗口升降调谐；采样窗口大小与失败率阈值均来自 `AutoTuneOptions`。
- `DangerZoneExecutor.cs`：危险路径统一走隔离器（超时/重试/熔断），弹性参数来自 `DangerZoneOptions`。
- - `SortingTaskTraceWriter.cs`：按分表后缀分组写入，并将执行结果回传给调谐器。
- - `SyncTaskConfigRepository.cs`：从 `SyncJob` 配置节读取表定义，校验 `StartTimeLocal` 禁止 `Z` 与 offset，校验 `ExcludedColumns` 不得与 `UniqueKeys`、`CursorColumn`、软删除关键列冲突，并解析优先级与多表并发上限。
- `OracleSourceReader.cs`：源端读取器基础实现，支持按窗口分页读取与按窗口读取业务键集合，并在分页读取阶段过滤 `ExcludedColumns`；同时强制校验 `SourceSchema/SourceTable` 安全标识符，确保外部 Oracle 只读链路安全。
- `SyncStagingRepository.cs`：暂存仓储基础实现，按 `BatchId + PageNo` 进行内存暂存，并在写入阶段过滤 `ExcludedColumns`。
- `SyncUpsertRepository.cs`：幂等合并基础实现，支持 `UniqueKeys` 下插入/覆盖更新/一致跳过，并在合并比较/写入阶段过滤 `ExcludedColumns`；同时提供目标键删除能力（软删/硬删）与目标端文件持久化落地能力。业务键构建直接使用 `SyncBusinessKeyBuilder.Build`。
- `SyncDeletionRepository.cs`：删除同步仓储基础实现，支持窗口内源端键集合与目标键集合差异识别，并按策略执行删除。
- `ShardTableResolver.cs`：分表解析仓储实现，按逻辑表枚举物理分表并解析分表月份后缀。
- `ShardRetentionRepository.cs`：分表保留期仓储实现，在危险动作隔离器保护下执行分表删除并输出审计日志，且可基于系统元数据生成可回放回滚 DDL。
- `SyncCheckpointRepository.cs`：检查点文件持久化实现，支持失败后续跑；读取失败时抛出异常，避免静默回退引发窗口误回溯；序列化选项复用静态只读字段，读写日志均以 Information 级落盘；写入改为临时文件 + File.Replace/Move 原子替换，防止崩溃产生半写 JSON。
- `SyncBatchRepository.cs`：同步批次仓储基础实现，支持 `Pending/InProgress/Completed/Failed` 状态流转与最近失败批次查询。
- `SyncChangeLogRepository.cs`：同步变更日志仓储基础实现，支持批量写入审计记录。
- `SyncDeletionLogRepository.cs`：同步删除日志仓储基础实现，支持批量写入删除审计记录（含 DryRun 执行标记）。
- `ServiceCollectionExtensions.cs`：统一注册基础设施依赖。
- `202603280001_InitialHubSchema.cs`：基础表结构迁移。
- `nlog.config`：NLog 日志配置，输出至控制台与滚动日志文件（按日切割，保留 30 天）。
- - `SyncBackgroundWorker.cs`：同步后台任务，按 `SyncJob.PollingIntervalSeconds` 周期触发全部启用表同步。
- `RetentionBackgroundWorker.cs`：保留期后台任务，按 `RetentionJob.PollingIntervalSeconds` 周期触发分表保留期治理。
- `EFCore手动迁移操作指南.md`：提供手工迁移、脚本导出、回滚、排障流程。
- `当前程序能力与缺陷分析.md`：汇总当前程序能力、功能清单、代码缺陷与逻辑 BUG，作为后续修复与优化输入。
- `Oracle到SQLServer同步架构设计.md`：定义外部 Oracle DB First 只读同步到本地 SQL Server 的详细落地方案，包含接口与实现命名、配置模型、可配置排除列、幂等覆盖、删除同步、分表保留期治理与验收清单。
- `Oracle到SQLServer同步实施计划.md`：按 PR 拆分同步架构落地步骤（最多 6 个 PR）的进度跟踪文档，定义完成项删除前必须通读代码确认完全实现的维护规则，随实施进展动态更新。

## 可继续完善内容
- 将 PR-1 基础实现继续推进到真实数据库落地实现（真实 Oracle 读取、SQL Server 暂存表与 MERGE 对接）。
- 将同步指标从日志输出升级为可接入监控平台的统一指标管道（如 Prometheus/OpenTelemetry）。
