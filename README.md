# EverydayChain.Hub

## 本次更新内容
- 自动迁移入口新增“连接安全参数快照”日志（不脱敏）：启动迁移前输出完整 ConnectionString 及 DataSource、InitialCatalog、Encrypt、TrustServerCertificate、IntegratedSecurity、ConnectTimeout，辅助定位 SQL Server pre-login 握手失败与 TLS/证书配置不匹配问题。
- 启动自动迁移异常诊断增强：当 EF Core `RetryLimitExceededException` 内层为 SQL Server `SqlException(10054)` 时，输出面向运维的握手失败排查指引（TLS 兼容、Encrypt/TrustServerCertificate、网络中断、数据库重启/高负载）及 `ClientConnectionId`，便于快速定位根因。
- 同步契约目录按 DDD 规范收敛：`EverydayChain.Hub.Application/Repositories` 已迁移为 `EverydayChain.Hub.Application/Abstractions/Persistence`，并完成全仓引用更新。
- 修复 `Worker`、`SyncBackgroundWorker`、`RetentionBackgroundWorker` 的轮询延迟取消路径：在 `Task.Delay` 处显式捕获取消异常并正常退出，避免停止阶段抛出未处理取消异常导致流程噪声。
- 日志落盘策略保持不变：业务日志继续通过 NLog `file` target 落盘，单文件上限维持 10MB 滚动阈值。
- 分表预建逻辑改为仅从 `SyncJob.Tables` 启用项的 `TargetLogicalTable` 动态推导，支持多逻辑表并行预建。
- 启动阶段新增逻辑表名集合构建与校验：去重、去空白、仅允许字母/数字/下划线，空集合直接抛出可读配置异常。
- `ShardTableProvisioner` 改为按“逻辑表 × 月份后缀”组合预建分表，危险建表动作仍统一通过 `IDangerZoneExecutor` 隔离执行。
- `ShardingOptions` 与 `appsettings.json` 移除 `BaseTableName`、`ManagedLogicalTables`，仅保留连接与预建月数配置。
- 危险操作隔离器新增“按操作覆盖超时”能力：默认超时仍由 `DangerZone.TimeoutSeconds` 控制，自动迁移单独使用 `DangerZone.AutoMigrateTimeoutSeconds`（默认 180 秒），用于降低冷启动/数据库唤醒场景下 30 秒超时导致的启动失败概率。

## 解决方案文件树与职责
```text
.
├── EverydayChain.Hub.sln
├── README.md
├── EFCore手动迁移操作指南.md
├── 持续运行一年稳定性改造清单.md
├── 监控告警规则基线清单.md
├── 年度维护清单.md
├── 值班处置手册.md
├── 当前程序能力与缺陷分析.md
├── Oracle到SQLServer同步架构设计.md
├── Oracle到SQLServer同步实施计划.md
├── scripts
│   ├── health-check.sh
│   ├── disaster-recovery.sh
│   └── stability-drill.sh
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
│   ├── Options/ShardingOptions.cs
│   ├── Options/SyncJobOptions.cs
│   ├── Options/RetentionJobOptions.cs
│   └── Options/OracleOptions.cs
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
│       ├── LogicalTableNameNormalizer.cs
│       ├── RuntimeStoragePathResolver.cs
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
│       ├── IRuntimeStorageGuard.cs
│       ├── RuntimeStorageGuard.cs
│       ├── IAutoMigrationService.cs
│       ├── AutoMigrationService.cs
│       ├── AutoMigrationHostedService.cs
│       ├── IShardTableProvisioner.cs
│       ├── ShardTableProvisioner.cs
│       ├── ISqlExecutionTuner.cs
│       ├── SqlExecutionTuner.cs
│       ├── ISortingTaskTraceWriter.cs
│       └── SortingTaskTraceWriter.cs
├── EverydayChain.Hub.Tests
│   ├── EverydayChain.Hub.Tests.csproj
│   └── Services
│       ├── ServiceCollectionExtensionsTests.cs
│       └── SyncWindowCalculatorTests.cs
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
- `scripts/health-check.sh`：一键体检脚本，检查磁盘空间、目录权限、关键文件可读写、配置文件格式、日志健康状态、进程存活与压缩归档文件状态，可集成到监控或定时任务。
- `scripts/disaster-recovery.sh`：灾难恢复脚本，支持检查点重置（checkpoint-reset）、快照从归档恢复（snapshot-restore）、快照备份（snapshot-backup）、归档清理（archive-cleanup）与完全重置（full-reset）；全部操作支持 --dry-run 预览模式。
- `scripts/stability-drill.sh`：稳定性演练脚本，串联体检与灾备动作（checkpoint-reset、snapshot-backup、snapshot-restore、archive-cleanup），支持 dry-run 与真实执行并自动生成演练记录。
- `监控告警规则基线清单.md`：监控告警规则基线文档，定义日志关键字告警、指标阈值告警与演练留档验收口径，用于补齐稳定性清单剩余交付项。
- `年度维护清单.md`：月度/季度/年度例行巡检项标准化清单，包含磁盘治理、日志审查、数据一致性、配置审核、灾难恢复演练、容量规划、安全审计等条目及快速异常处理参考表。
- `值班处置手册.md`：日常值班与告警应急处置手册，覆盖 9 类告警的处置步骤（卡死检测、磁盘不足、内存水位、整轮超时、熔断、检查点损坏、快照损坏、归档失败、进程停止），定义 P0~P3 优先级与升级规则，含处置记录与演练记录模板。
- `SyncTableDefinition.cs` / `SyncWindow.cs` / `SyncCheckpoint.cs` / `SyncBatchResult.cs`：定义同步链路执行、窗口与结果统计的核心领域模型。
- `SyncBatch.cs` / `SyncChangeLog.cs` / `SyncDeletionLog.cs`：定义批次状态跟踪、变更审计与删除审计的数据模型。
- `SyncBusinessKeyBuilder.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：同步业务键构建共享组件，按 `UniqueKeys` 配置将行数据拼接为 `|` 分隔的业务键文本，供 Upsert 与删除识别阶段统一调用。
- `SyncColumnFilter.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：同步列过滤共享组件，提供 `ExcludedColumns` 规范化与行级过滤能力，并统一维护软删除关键列常量。
- `RuntimeStoragePathResolver.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：运行期路径解析共享组件，统一解析检查点、目标快照与存储守护所需的绝对路径。
- `LogicalTableNameNormalizer.cs`（`EverydayChain.Hub.SharedKernel/Utilities`）：逻辑表名规范化与安全校验共享组件，统一执行去空白、SQL 标识符校验与异常信息输出。
- `SyncMode.cs` / `DeletionPolicy.cs` / `LagControlMode.cs` / `SyncBatchStatus.cs` / `SyncChangeOperationType.cs` / `SyncTablePriority.cs`：同步模式、删除策略、滞后控制、批次状态、变更操作类型与调度优先级枚举，均含中文 XML 注释与 `Description`。
- `EverydayChain.Hub.Domain/Options/*.cs`：统一承载全部配置实体（`Worker`、`Sharding`、`AutoTune`、`DangerZone`、`SyncJob`、`RetentionJob`、`Oracle` 等），供 Host/Infrastructure 绑定读取。
- `SortingTaskTraceEntity.cs`：可分表的写入实体，承载中台追踪数据；所有属性均含 XML 注释。
- `SyncExecutionContext.cs` + `SyncReadRequest.cs` + `SyncReadResult.cs` + `SyncMergeRequest.cs` + `SyncMergeResult.cs` + `SyncDeletionDetectRequest.cs` + `SyncDeletionApplyRequest.cs` + `SyncDeletionExecutionResult.cs` + `SyncDeletionCandidate.cs` + `SyncKeyReadRequest.cs` + `SyncTargetStateRow.cs`：同步执行、删除识别与轻量幂等状态存储的数据契约模型。
- `Application/Abstractions/Persistence/ISyncBatchRepository.cs` / `ISyncChangeLogRepository.cs` / `ISyncDeletionRepository.cs` / `ISyncDeletionLogRepository.cs`：定义批次状态、变更日志、删除识别执行与删除日志写入契约。
- `Application/Abstractions/Persistence/IShardTableResolver.cs` / `IShardRetentionRepository.cs`：定义分表识别与分表清理执行契约（含分表完整回滚脚本生成）。
- `ISyncOrchestrator.cs` / `SyncOrchestrator.cs`：同步任务编排入口，负责读取配置、加载检查点、计算窗口，并基于优先级与并发上限执行多表同步。
- `ISyncWindowCalculator.cs` / `SyncWindowCalculator.cs`：根据 `CursorColumn + StartTimeLocal` 与检查点计算本地增量窗口，并对时钟回拨与 DST 非法本地时刻执行窗口边界保护。
- `IDeletionExecutionService.cs` / `DeletionExecutionService.cs`：执行删除识别、删除策略应用（含 DryRun）并生成删除审计与删除变更日志。
- `IRetentionExecutionService.cs` / `RetentionExecutionService.cs`：执行分表保留期治理，完成过期分表识别、完整回滚脚本生成、dry-run 审计、删除执行、失败隔离与汇总。
- `ISyncExecutionService.cs` / `SyncExecutionService.cs`：执行分页读取、暂存、幂等合并、删除同步、日志写入、检查点提交，并输出延迟/积压/吞吐/失败率指标日志；异常场景输出 NLog 错误日志。
- `HubDbContext.cs`：根据分表后缀动态映射表名。
- `TableSuffixScope.cs` + `ShardModelCacheKeyFactory.cs`：保证不同后缀下 EF Model 能正确缓存隔离。
- `MonthShardSuffixResolver.cs`：按月份生成分表后缀（如 `_202603`）。
- `IShardTableProvisioner.cs` + `ShardTableProvisioner.cs`：在 SQL Server 中按需创建分表与索引（不存在才建）。
- `AutoMigrationService.cs` + `AutoMigrationHostedService.cs`：应用启动时自动执行 `Migrate` 与分表预创建。
- `SqlExecutionTuner.cs`：基于失败率和耗时进行批量窗口升降调谐；采样窗口大小与失败率阈值均来自 `AutoTuneOptions`。
- `DangerZoneExecutor.cs`：危险路径统一走隔离器（超时/重试/熔断），弹性参数来自 `DangerZoneOptions`。
- `IRuntimeStorageGuard.cs` + `RuntimeStorageGuard.cs`：运行期存储守护服务，负责启动阶段的磁盘空间、目录权限、关键文件可读写自检，并在检查点/目标快照写入前执行磁盘阈值校验与告警阻断；同时提供单表内存水位监控与节流告警能力。
- `SortingTaskTraceWriter.cs`：按分表后缀分组写入，并将执行结果回传给调谐器。
- `SyncTaskConfigRepository.cs`：从 `SyncJob` 配置节读取表定义，校验 `StartTimeLocal` 禁止 `Z` 与 offset，校验 `ExcludedColumns` 不得与 `UniqueKeys`、`CursorColumn`、软删除关键列冲突，并解析优先级与多表并发上限。
- `OracleOptions.cs`：远端 Oracle 连接配置实体，定义连接字符串、默认 Schema、只读开关、命令超时与分页上限。
- `OracleSourceReader.cs`：源端读取器 Oracle 实现，使用参数化 SQL 执行真实只读查询，支持分页增量读取、业务键读取、`ExcludedColumns` 过滤，并在异常场景输出错误日志。
- `SyncStagingRepository.cs`：暂存仓储基础实现，按 `BatchId + PageNo` 进行内存暂存，并在写入阶段过滤 `ExcludedColumns`。
- `SyncUpsertRepository.cs`：幂等合并基础实现，支持 `UniqueKeys` 下插入/覆盖更新/一致跳过，并在合并阶段过滤 `ExcludedColumns`；目标端持久化改为按表分片的轻量幂等状态（业务键、行摘要、游标、软删标记），不保留旧版全量快照兼容迁移；支持目标键删除能力（软删/硬删）、空闲驱逐与 GZip 压缩归档策略。
- `SyncDeletionRepository.cs`：删除同步仓储基础实现，基于轻量幂等状态执行窗口过滤与源端键差异识别，并按策略执行删除。
- `ShardTableResolver.cs`：分表解析仓储实现，按逻辑表枚举物理分表并解析分表月份后缀。
- `ShardRetentionRepository.cs`：分表保留期仓储实现，在危险动作隔离器保护下执行分表删除并输出审计日志，且可基于系统元数据生成可回放回滚 DDL。
- `SyncCheckpointRepository.cs`：检查点文件持久化实现，读写日志均以 Information 级落盘；写入改为临时文件 + File.Replace/Move 原子替换，防止崩溃产生半写 JSON。
- `SyncBatchRepository.cs`：同步批次仓储基础实现，支持 `Pending/InProgress/Completed/Failed` 状态流转与最近失败批次查询。
- `SyncChangeLogRepository.cs`：同步变更日志仓储基础实现，支持批量写入审计记录。
- `SyncDeletionLogRepository.cs`：同步删除日志仓储基础实现，支持批量写入删除审计记录（含 DryRun 执行标记）。
- `ServiceCollectionExtensions.cs`：统一注册基础设施依赖，并在启动阶段从启用同步表配置提取逻辑表名集合，完成安全校验与空配置异常拦截。
- `202603280001_InitialHubSchema.cs`：基础表结构迁移。
- `nlog.config`：NLog 日志配置，输出至控制台与滚动日志文件（按日切割，单文件上限 10 MB，保留 30 天）。
- `SyncBackgroundWorker.cs`：同步后台任务，按 `SyncJob.PollingIntervalSeconds` 周期触发全部启用表同步；支持表级超时保护（`TableSyncTimeoutSeconds`）；内置看门狗卡死检测（`WatchdogTimeoutSeconds`，主循环超过阈值未推进时输出 Critical 日志）；每轮输出整体汇总指标日志（总表数、失败表数、整体失败率、最大滞后/积压、轮次耗时）。
- `RetentionBackgroundWorker.cs`：保留期后台任务，按 `RetentionJob.PollingIntervalSeconds` 周期触发分表保留期治理。
- `EverydayChain.Hub.Tests/Services/SyncWindowCalculatorTests.cs`：SyncWindowCalculator 时间窗口回归测试套件（12 个测试用例，覆盖正常窗口、时钟回拨冻结、UTC 拒绝、Unspecified Kind 兼容、时钟扰动组合场景）。
- `EverydayChain.Hub.Tests/Services/ServiceCollectionExtensionsTests.cs`：逻辑表名构建测试，覆盖非法标识符与空启用集合异常场景。
- `EFCore手动迁移操作指南.md`：提供手工迁移、脚本导出、回滚、排障流程。
- `持续运行一年稳定性改造清单.md`：面向"连续运行一年"目标的稳定性改造清单，按 P0/P1/P2 组织改造优先级、待确认项与验收标准。
- `年度维护清单.md`：月度/季度/年度例行巡检清单，覆盖磁盘、日志、数据一致性、配置审核、依赖升级、灾难恢复与安全审计。
- `当前程序能力与缺陷分析.md`：汇总当前程序能力、功能清单、代码缺陷与逻辑 BUG，作为后续修复与优化输入。
- `Oracle到SQLServer同步架构设计.md`：定义外部 Oracle DB First 只读同步到本地 SQL Server 的详细落地方案。
- `Oracle到SQLServer同步实施计划.md`：按 PR 拆分同步架构落地步骤的进度跟踪文档。
- `ShardingOptions.cs`：分表配置模型，仅保留连接、Schema 与自动预建月数等基础配置。
- `ShardTableProvisioner.cs`：分表预建实现，按启用同步表推导的逻辑表与后缀笛卡尔组合执行建表，并保持危险动作隔离执行。
- `AutoMigrationService.cs`：应用启动迁移入口，保持原调用语义，通过分表预建器自动覆盖多逻辑表。
- `appsettings.json`：主配置样例，移除分表逻辑表名静态配置，统一由 `SyncJob.Tables.TargetLogicalTable` 提供。

## 可继续完善内容（本次 PR 后续行动项）
- 增加 `Sharding` 配置级“连接握手预检”开关，在执行 `Migrate` 前进行一次可观测的只连接不迁移探测，并将失败分类写入统一指标。
- 在 `DangerZoneExecutor` 中补充按异常类型的统一诊断标签（如网络抖动、认证失败、超时）并输出结构化字段，降低跨环境排障成本。
- 对“全仓一键体检并自动修复”的大范围需求，建议拆分为多批次 PR（性能热点、规则一致性、接口收敛、文档治理）逐项推进并逐项验收。
- 在启动日志中输出纳管逻辑表集合快照，便于运维核对多表预建范围。
- 为 `DangerZone` 增加“按操作名配置超时映射”能力（如 `OperationTimeoutOverrides`），避免未来新增长耗时动作时需要再次改动代码。
