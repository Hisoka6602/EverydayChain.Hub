# Oracle 到 SQLServer 同步实施计划

> 执行依据：[Oracle到SQLServer同步架构设计.md](Oracle到SQLServer同步架构设计.md)
> 文档用途：按 PR 拆分同步架构落地步骤的进度跟踪文档。
> 强制规则：删除本文档任一条目前，必须先通读相关代码并确认"已完全实现 + 行为可用 + 验收通过"；未完全实现不得删除该条目。

---

## 一、阶段一：最小可用（增量读取 + 幂等覆盖 + 检查点）

| 条目 | 描述 | 实现状态 | 对应文件/PR |
|---|---|:---:|---|
| 1.1 | 领域模型：`SyncTableDefinition`、`SyncWindow`、`SyncCheckpoint`、`SyncBatch`、`SyncBatchResult`、`SyncChangeLog`、`SyncDeletionLog` | ✅ 已完成 | `Domain/Sync/` |
| 1.2 | 配置仓储：`SyncTaskConfigRepository` 支持 `CursorColumn`、`UniqueKeys`、`StartTimeLocal`、`ExcludedColumns`、`DeletionPolicy`、`LagControl` | ✅ 已完成 | `Infrastructure/Repositories/SyncTaskConfigRepository.cs` |
| 1.3 | 源端读取：`OracleSourceReader.ReadIncrementalPageAsync` 按增量字段分页读取 | ✅ 已完成 | `Infrastructure/Repositories/OracleSourceReader.cs` |
| 1.4 | 本地暂存：`SyncStagingRepository` 支持分页写入与清理 | ✅ 已完成 | `Infrastructure/Repositories/SyncStagingRepository.cs` |
| 1.5 | 幂等合并：`SqlServerSyncUpsertRepository.MergeCoreAsync` 按唯一键 Insert/Update/Skip | ✅ 已完成 | `Infrastructure/Repositories/SqlServerSyncUpsertRepository.cs` |
| 1.6 | 检查点持久化：`SyncCheckpointRepository` 文件读写 + 断点续跑 | ✅ 已完成 | `Infrastructure/Repositories/SyncCheckpointRepository.cs` |
| 1.7 | 批次日志：`SyncBatchRepository`（SQL Server 持久化 + 自动分表 + 自动迁移） | ✅ 已完成 | `Infrastructure/Repositories/SyncBatchRepository.cs` |
| 1.8 | 变更日志：`InMemorySyncChangeLogRepository`（内存有界缓冲，上限 200,000 条） | ✅ 已完成 | `Infrastructure/Repositories/InMemorySyncChangeLogRepository.cs` |
| 1.9 | 应用层编排：`SyncOrchestrator`、`SyncWindowCalculator`、`SyncExecutionService` | ✅ 已完成 | `Application/Services/` |
| 1.10 | Host 后台任务：`SyncBackgroundWorker` | ✅ 已完成 | `Host/Workers/SyncBackgroundWorker.cs` |

---

## 二、阶段二：删除与治理

| 条目 | 描述 | 实现状态 | 对应文件/PR |
|---|---|:---:|---|
| 2.1 | 删除识别：基于业务键比对源端与目标端差异，生成删除候选列表 | ✅ 已完成 | `Application/Services/DeletionExecutionService.cs` |
| 2.2 | 删除策略：支持 `Disabled`/`SoftDelete`/`HardDelete` 三种策略 | ✅ 已完成 | `Domain/Enums/DeletionPolicy.cs`、`SqlServerSyncUpsertRepository.cs` |
| 2.3 | 删除日志：`InMemorySyncDeletionLogRepository`（内存有界缓冲，上限 200,000 条） | ✅ 已完成 | `Infrastructure/Repositories/InMemorySyncDeletionLogRepository.cs` |
| 2.4 | 危险动作门禁：`DangerZoneExecutor` 提供开关、dry-run、审计能力 | ✅ 已完成 | `Infrastructure/Services/DangerZoneExecutor.cs` |
| 2.5 | 回滚支持：`scripts/disaster-recovery.sh` 支持检查点重置、快照恢复、干净重置 | ✅ 已完成 | `scripts/disaster-recovery.sh` |
| 2.6 | 软删时间倒置修复：`MarkSoftDeletedStateAsync` 统一捕获单次 `DateTime.Now` 避免 `UpdatedTimeLocal < SoftDeletedTimeLocal` | ✅ 已完成（P1-001） | `Infrastructure/Repositories/SqlServerSyncUpsertRepository.cs` |

---

## 三、阶段三：多表并发 + 分表保留期治理

| 条目 | 描述 | 实现状态 | 对应文件/PR |
|---|---|:---:|---|
| 3.1 | 多表并发调度：`SyncOrchestrator.RunAllEnabledTableSyncAsync` 按优先级并发执行、单表失败不阻塞其余表 | ✅ 已完成 | `Application/Services/SyncOrchestrator.cs` |
| 3.2 | 分表策略：`MonthShardSuffixResolver` 按月分表，`ShardModelCacheKeyFactory` 动态模型缓存 | ✅ 已完成 | `Infrastructure/Persistence/Sharding/` |
| 3.3 | 分表建表：`ShardTableProvisioner` 按表代码+月份自动建表 | ✅ 已完成 | `Infrastructure/Services/ShardTableProvisioner.cs` |
| 3.4 | 保留期治理：`ShardRetentionRepository` 识别并清理过期分表 | ✅ 已完成 | `Infrastructure/Repositories/ShardRetentionRepository.cs` |
| 3.5 | 保留期后台任务：`RetentionBackgroundWorker` | ✅ 已完成 | `Host/Workers/RetentionBackgroundWorker.cs` |
| 3.6 | 延迟/积压/错误率指标：`SyncBackgroundWorker` 整轮汇总日志（`MaxLagMinutes`、`MaxBacklogMinutes`、`OverallFailureRate`） | ✅ 已完成 | `Host/Workers/SyncBackgroundWorker.cs` |

---

## 四、阶段四：StatusDriven 可切换同步模式

| 条目 | 描述 | 实现状态 | 对应文件/PR |
|---|---|:---:|---|
| 4.1 | `SyncMode` 枚举：`KeyedMerge`/`StatusDriven`，空值默认 `KeyedMerge` | ✅ 已完成 | `Domain/Enums/SyncMode.cs` |
| 4.2 | 状态读取：`OracleStatusDrivenSourceReader` 按 `StatusColumnName=PendingStatusValue` 分页拉取，携带 `__RowId` | ✅ 已完成 | `Infrastructure/Sync/Readers/OracleStatusDrivenSourceReader.cs` |
| 4.3 | 追加写入：`SqlServerAppendOnlyWriter` 仅追加不 Upsert、不删除 | ✅ 已完成 | `Infrastructure/Sync/Writers/SqlServerAppendOnlyWriter.cs` |
| 4.4 | 远端状态回写：`OracleRemoteStatusWriter` 按 `ROWID` 更新状态列 + 审计列 | ✅ 已完成 | `Infrastructure/Sync/Writers/OracleRemoteStatusWriter.cs` |
| 4.5 | 消费服务：`RemoteStatusConsumeService` 编排读取→写入→回写闭环；`ShouldWriteBackRemoteStatus=true` 时固定读第 1 页避免跳行 | ✅ 已完成 | `Infrastructure/Sync/Services/RemoteStatusConsumeService.cs` |
| 4.6 | 配置：`RemoteStatusConsumeProfile`、`StatusDriven` 配置段含 `WriteBackCompletedTimeColumnName`/`WriteBackBatchIdColumnName` 审计列 | ✅ 已完成 | `Domain/Sync/Models/RemoteStatusConsumeProfile.cs`、`Domain/Options/SyncTableOptions.cs` |
| 4.7 | 接口归属修正：`IOracleRemoteStatusWriter`、`IOracleStatusDrivenSourceReader`、`ISqlServerAppendOnlyWriter` 迁移至 `Application/Abstractions/Sync/` | ✅ 已完成（P2-007） | `Application/Abstractions/Sync/` |

---

## 五、架构设计验收清单（对应设计文档第 13 节）

| 验收项 | 状态 |
|---|:---:|
| 已支持按 `CursorColumn + StartTimeLocal` 定义增量起点 | ✅ |
| 已支持按表配置 `UniqueKeys`，验证"重复覆盖不重复插入" | ✅ |
| 已实现外部删除到本地删除同步 | ✅ |
| 已实现删除记录（`SyncDeletionLog`）与变更记录（`SyncChangeLog`） | ✅ |
| 已实现本地按分表保留期清理 | ✅ |
| 已支持 `MaxLagMinutes`，控制实时误差 | ✅ |
| 已实现检查点续跑，任务失败后从上次成功位点恢复 | ✅ |
| 已实现危险动作门禁：开关、dry-run、审计、回滚脚本 | ✅ |
| 已确认外部 Oracle 全程只读，无 DDL/DML 写入 | ✅ |

---

## 六、待完善事项

> 以下条目为未来可优化方向，当前版本未列入强制实施范围。

| 条目 | 描述 | 优先级 |
|---|---|:---:|
| 6.1 | 增量落盘/分片持久化：优化目标快照从整表重写改为增量写入，降低大表 I/O | P1 |
| 6.2 | 内存仓储持久化：`InMemorySyncChangeLogRepository`、`InMemorySyncDeletionLogRepository` 当前为纯内存，进程重启后日志丢失；如需持久化需另行设计 | P2 |
| 6.3 | Polly 熔断策略可观测：当前熔断状态不对外暴露指标，后续可接入健康检查端点 | P2 |
| 6.4 | 扫描/落格字段强制回写规则落地：整件与拆零表在扫描阶段落实 `LENGTH/WIDTH/HIGH/CUBE/GROSSWEIGHT` 本地+远端写入，`SCANCOUNT` 按条码 `+1`，`CLOSETIME` 更新最新扫描时间；落格阶段落实 `STATUS` 本地+远端写入 | P1 |
