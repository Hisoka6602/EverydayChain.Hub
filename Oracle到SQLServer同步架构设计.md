# Oracle 到本地 SQL Server 同步架构设计（外部 DB First + ORM）

## 1. 文档目标

本文档用于指导后续开发“外部 Oracle → 本地 SQL Server”的同步系统，重点解决以下需求：

1. 使用 ORM 同步外部数据到本地 SQL Server。
2. 支持按“指定字段 + 指定开始时间”启动增量同步。
3. 支持为每张表配置唯一键，保证本地不重复；重复时覆盖更新。
4. 外部已删除的数据需要同步删除到本地，并保留删除记录与变更记录。
5. 支持本地按分表策略做数据保留期清理（例如仅保留 3 个月）。
6. 在“减少外部往返”的前提下，保证可配置的准实时（例如误差控制在最近 10 分钟内）。

---

## 2. 约束与原则

### 2.1 约束

1. 外部 Oracle 属于第三方系统，DB First，不允许结构改造。
2. 外部 Oracle 仅允许只读访问（SELECT），禁止写入与 DDL。
3. 本地 SQL Server 为可控库，且采用分表设计。
4. 时间语义统一使用本地时间，配置解析按本地时间处理。

### 2.2 设计原则

1. **外部只读、内部可控**：外部只拉取，本地完成幂等、合并、审计、清理。
2. **配置驱动**：所有表同步行为（唯一键、起始时间、轮询周期、保留期）配置化。
3. **高效增量优先**：优先按增量字段读取，避免频繁全表扫描。
4. **可恢复**：使用检查点机制，失败后从上次成功位点续跑。
5. **可审计**：任何插入、更新、删除都落变更记录；删除单独落删除记录。

---

## 3. 总体架构

采用 6 层流水线：

1. **任务配置层（Sync Task Config）**
2. **源端读取层（Oracle ORM Reader）**
3. **本地暂存层（SQLServer Staging）**
4. **本地合并层（Upsert + Delete）**
5. **记录审计层（ChangeLog + DeletionLog + BatchLog）**
6. **保留期治理层（分表清理）**

流程总览：

1. 调度器按表任务触发。
2. 读取任务配置与检查点，计算本次同步窗口（开始/结束时间）。
3. 从 Oracle 按增量窗口分页拉取到本地暂存。
4. 基于唯一键从暂存合并到目标分表（插入/覆盖更新）。
5. 基于“源端存在性对比”执行本地删除（可开关 + dry-run）。
6. 写入批次日志、变更日志、删除日志，更新检查点。
7. 定时执行保留期清理（按表删除过期分表）。

---

## 4. 分层与命名（接口 + 实现）

> 命名采用“接口 `I*` + 实现 `*`”风格，按职责拆分，避免单类过重。

## 4.1 Domain（领域层）

### 4.1.1 聚合与值对象

- `SyncTableDefinition`：单表同步定义（源表、目标逻辑表、唯一键、增量字段等）。
- `SyncWindow`：同步窗口（`WindowStartLocal`、`WindowEndLocal`）。
- `SyncCheckpoint`：检查点（上次成功游标、上次批次号、上次成功时间）。
- `SyncBatchResult`：单批次统计结果（读取/插入/更新/删除/跳过/耗时）。

### 4.1.2 领域枚举（放在 `EverydayChain.Hub.Domain.Enums`）

- `SyncMode`：`InitialFull` / `Incremental`
- `DeletionPolicy`：`Disabled` / `SoftDelete` / `HardDelete`
- `LagControlMode`：`FixedDelayWindow` / `DynamicDelayWindow`

## 4.2 Application（应用层）

### 4.2.1 应用服务接口

- `ISyncOrchestrator`
  - `RunTableSyncAsync(string tableCode, CancellationToken ct)`
  - `RunAllEnabledTableSyncAsync(CancellationToken ct)`

- `ISyncWindowCalculator`
  - `CalculateWindow(SyncTableDefinition definition, SyncCheckpoint checkpoint, DateTime nowLocal)`

- `ISyncExecutionService`
  - `ExecuteBatchAsync(SyncExecutionContext context, CancellationToken ct)`

- `IDeletionExecutionService`
  - `ExecuteDeletionAsync(SyncExecutionContext context, CancellationToken ct)`

- `IRetentionExecutionService`
  - `ExecuteRetentionCleanupAsync(CancellationToken ct)`

### 4.2.2 应用服务实现

- `SyncOrchestrator`
- `SyncWindowCalculator`
- `SyncExecutionService`
- `DeletionExecutionService`
- `RetentionExecutionService`

## 4.3 Infrastructure（基础设施层）

### 4.3.1 配置仓储

- `ISyncTaskConfigRepository`
  - 读取每张表的同步配置。
- 实现：`SyncTaskConfigRepository`

### 4.3.2 源端读取（Oracle + ORM）

- `IOracleSourceReader`
  - `ReadIncrementalPageAsync(SyncReadRequest request, CancellationToken ct)`
  - `ReadByKeysAsync(SyncKeyReadRequest request, CancellationToken ct)`
- 实现：`OracleSourceReader`

> ORM 实现建议：
>
> - 使用独立 `OracleReadDbContext`（只读连接字符串）。
> - 实体使用 DB First 生成，禁止修改源表结构。
> - 对多表通用读取采用表达式拼装 + 映射配置，避免硬编码 SQL 拼接。

### 4.3.3 本地暂存

- `ISyncStagingRepository`
  - `BulkInsertAsync(...)`
  - `ClearBatchAsync(...)`
- 实现：`SyncStagingRepository`

### 4.3.4 本地合并（幂等 Upsert）

- `ISyncUpsertRepository`
  - `MergeFromStagingAsync(SyncMergeRequest request, CancellationToken ct)`
- 实现：`SyncUpsertRepository`

### 4.3.5 删除同步

- `ISyncDeletionRepository`
  - `DetectDeletedKeysAsync(SyncDeletionDetectRequest request, CancellationToken ct)`
  - `ApplyDeletionAsync(SyncDeletionApplyRequest request, CancellationToken ct)`
- 实现：`SyncDeletionRepository`

### 4.3.6 检查点与批次状态

- `ISyncCheckpointRepository`
  - `GetAsync(...)`
  - `SaveAsync(...)`
- 实现：`SyncCheckpointRepository`

- `ISyncBatchRepository`
  - `CreateBatchAsync(...)`
  - `CompleteBatchAsync(...)`
  - `FailBatchAsync(...)`
- 实现：`SyncBatchRepository`

### 4.3.7 变更日志与删除日志

- `ISyncChangeLogRepository`
  - `WriteChangesAsync(...)`
- 实现：`SyncChangeLogRepository`

- `ISyncDeletionLogRepository`
  - `WriteDeletionsAsync(...)`
- 实现：`SyncDeletionLogRepository`

### 4.3.8 分表与保留期治理

- `IShardTableResolver`
  - `ResolvePhysicalTableName(string logicalTable, DateTime dataTimeLocal)`
- 实现：`ShardTableResolver`

- `IShardRetentionRepository`
  - `ListExpiredShardTablesAsync(...)`
  - `DropShardTableAsync(...)`
- 实现：`ShardRetentionRepository`

## 4.4 Host（任务调度层）

- `SyncBackgroundWorker`：周期触发同步作业。
- `RetentionBackgroundWorker`：周期触发分表保留期清理。
- `SyncJobOptions`：全局调度配置。
- `SyncTableOptions`：单表配置项。

---

## 5. 核心配置模型（必须可配置）

每张表一份配置：

```json
{
  "TableCode": "Order",
  "Enabled": true,
  "Source": {
    "Schema": "EXT",
    "Table": "T_ORDER"
  },
  "Target": {
    "LogicalTable": "Order"
  },
  "Sync": {
    "Mode": "Incremental",
    "CursorColumn": "LastModifiedTime",
    "StartTimeLocal": "2026-03-01 00:00:00",
    "PollingIntervalSeconds": 60,
    "MaxLagMinutes": 10,
    "PageSize": 5000,
    "MaxParallelPages": 2
  },
  "Identity": {
    "UniqueKeys": ["OrderId"]
  },
  "Delete": {
    "Policy": "HardDelete",
    "Enabled": true,
    "DryRun": false
  },
  "Retention": {
    "Enabled": true,
    "KeepMonths": 3
  }
}
```

配置解释：

1. `CursorColumn + StartTimeLocal`：定义从哪个字段、哪个本地时间开始同步。
2. `UniqueKeys`：定义幂等覆盖键（可单键/复合键）。
3. `MaxLagMinutes`：实时性控制，默认固定延迟窗口（例如 10 分钟）。
4. `Delete.Policy`：删除策略，支持关闭/软删/硬删。
5. `Retention.KeepMonths`：本地分表保留期（月）。

---

## 6. 同步流程详细设计

## 6.1 增量窗口计算（满足“实时 + 低往返”）

- `WindowStartLocal`：
  - 首次：取配置 `StartTimeLocal`。
  - 非首次：取检查点 `LastSuccessCursorLocal`。

- `WindowEndLocal`：
  - `NowLocal - MaxLagMinutes`。
  - 目的：避免读取到仍在源端事务中的“未稳定数据”。

- 查询条件：
  - `CursorColumn > WindowStartLocal AND CursorColumn <= WindowEndLocal`

该策略可在减少回查的同时，将误差限制在可配置窗口（如 10 分钟）。

## 6.2 读取与暂存

1. Oracle 按 `CursorColumn` + `UniqueKeys` 排序分页读取。
2. 单次尽量批量（例如 5000 行），减少往返次数。
3. 每页写入本地 `Staging`，记录 `BatchId`、`PageNo`、抓取时间。

## 6.3 幂等合并（防重复 + 覆盖）

按 `UniqueKeys` 合并规则：

1. 目标不存在：插入。
2. 目标存在且数据有差异：覆盖更新。
3. 目标存在且数据一致：跳过。

> 差异判定建议使用“字段比较 + 行摘要哈希”组合，减少不必要更新。

## 6.4 删除同步（外部删，本地也删）

删除同步分两步：

1. **识别删除**：在同步窗口内，本地目标键集合与源端键集合做存在性差异比对。
2. **执行删除**：
   - `SoftDelete`：更新 `IsDeleted` 与删除时间。
   - `HardDelete`：物理删除本地数据。

无论软删或硬删，都必须写：

1. `SyncDeletionLog`（删除记录）
2. `SyncChangeLog`（变更记录）

## 6.5 检查点提交

仅在“读取、合并、删除、日志写入”全部成功后提交检查点，保证批次原子可恢复。

---

## 7. 数据模型建议（本地元数据表）

## 7.1 `SyncCheckpoint`

- `TableCode`
- `LastSuccessCursorLocal`
- `LastBatchId`
- `LastSuccessTimeLocal`
- `LastError`

## 7.2 `SyncBatch`

- `BatchId`
- `TableCode`
- `WindowStartLocal`
- `WindowEndLocal`
- `ReadCount`
- `InsertCount`
- `UpdateCount`
- `DeleteCount`
- `SkipCount`
- `Status`（建议值：`Pending` / `InProgress` / `Completed` / `Failed`）
- `StartedTimeLocal`
- `CompletedTimeLocal`

## 7.3 `SyncChangeLog`

- `BatchId`
- `TableCode`
- `OperationType`（Insert/Update/Delete）
- `BusinessKey`
- `BeforeSnapshot`
- `AfterSnapshot`
- `ChangedTimeLocal`

## 7.4 `SyncDeletionLog`

- `BatchId`
- `TableCode`
- `BusinessKey`
- `DeletionPolicy`
- `DeletedTimeLocal`
- `SourceEvidence`

---

## 8. 分表保留期清理设计（按表删）

场景：本地为分表（如按月分表），只保留最近 3 个月。

策略：

1. `RetentionBackgroundWorker` 每天/每小时执行。
2. 通过 `IShardTableResolver` 获取逻辑表所有物理分表。
3. 按命名规则解析分表时间（如 `Order_202512`）。
4. 计算过期阈值（当前本地时间回溯 `KeepMonths`）。
5. 对过期分表执行删除（drop/truncate 依据策略）。

危险动作门禁（必须）：

1. 开关控制（总开关 + 表级开关）
2. dry-run 预演（仅记录不执行）
3. 审计记录（谁在何时删除了哪些表）
4. 回滚脚本（DDL 恢复预案）

---

## 9. 高效与实时性优化策略

## 9.1 降低外部往返

1. 按增量窗口拉取，避免频繁全量。
2. 批量分页 + 流式处理，减少单行读取。
3. 读字段最小化（仅同步字段 + 键字段 + 游标字段）。
4. 合并在本地完成，避免回写源端。

## 9.2 控制准实时

1. `PollingIntervalSeconds` 可配置（如 30s/60s/120s）。
2. `MaxLagMinutes` 可配置（如 10 分钟）。
3. 对高优先级表使用更短轮询周期。
4. 对低优先级表使用较长周期，减轻整体压力。

## 9.3 稳定性

1. 每表独立并发上限，避免拖垮 Oracle。
2. 短暂故障重试（指数退避 + 上限）。
3. 连续失败熔断，恢复后再自动重试。

---

## 10. 典型时序（单表一次增量）

1. `SyncBackgroundWorker` 触发 `ISyncOrchestrator.RunTableSyncAsync`。
2. `ISyncTaskConfigRepository` 读取 `SyncTableDefinition`。
3. `ISyncCheckpointRepository` 读取检查点。
4. `ISyncWindowCalculator` 计算本次窗口。
5. `IOracleSourceReader` 分页读取并写入 `ISyncStagingRepository`。
6. `ISyncUpsertRepository` 执行合并（插入/覆盖/跳过）。
7. `IDeletionExecutionService` + `ISyncDeletionRepository` 执行删除同步。
8. `ISyncChangeLogRepository`、`ISyncDeletionLogRepository` 写审计记录。
9. `ISyncBatchRepository` 完成批次。
10. `ISyncCheckpointRepository` 提交新检查点。

---

## 11. 异常与审计规范

1. 所有异常必须记录 NLog，日志包含 `TableCode`、`BatchId`、`Window`、`Checkpoint`。
2. 删除动作必须单独审计，且能回溯到批次。
3. dry-run 模式下同样写审计日志，标记 `Executed = false`。

---

## 12. 分阶段落地建议

## 阶段一（最小可用）

1. 落地 1 张关键表。
2. 打通“增量读取 + 幂等覆盖 + 检查点”。
3. 打通批次日志与变更日志。

## 阶段二（删除与治理）

1. 增加删除识别与删除执行。
2. 增加删除日志。
3. 增加 dry-run、审计、回滚脚本机制。

## 阶段三（多表与性能）

1. 多表配置化并发调度。
2. 增加分表保留期治理。
3. 增加延迟、积压、错误率告警。

---

## 13. 验收清单（Checklist）

- [ ] 已支持按 `CursorColumn + StartTimeLocal` 定义增量起点。
- [ ] 已支持按表配置 `UniqueKeys`，并验证“重复覆盖不重复插入”。
- [ ] 已实现外部删除到本地删除同步。
- [ ] 已实现删除记录（`SyncDeletionLog`）与变更记录（`SyncChangeLog`）。
- [ ] 已实现本地按分表保留期清理（例如仅保留 3 个月）。
- [ ] 已支持 `MaxLagMinutes`，并验证可控制在近实时误差范围（如 10 分钟）。
- [ ] 已实现检查点续跑，任务失败后可从上次成功位点恢复。
- [ ] 已实现危险动作门禁：开关、dry-run、审计、回滚脚本。
- [ ] 已确认外部 Oracle 全程只读，无 DDL/DML 写入。

---

## 14. 结论

该设计在不改造外部 Oracle 的前提下，实现了：

1. ORM 化同步能力（可持续扩展多表）。
2. 可配置的起始位点与准实时窗口。
3. 唯一键幂等覆盖、防重复写入。
4. 删除同步与全量审计可追溯。
5. 分表保留期治理与高效低往返读取。

可直接作为后续开发实现蓝图与接口落地基线。
