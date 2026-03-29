# Oracle 到 SQL Server 同步实施计划（按 PR 划分）

## 维护规则（强制）

1. 本计划用于跟踪《Oracle到SQLServer同步架构设计.md》的代码落地进度，最多拆分为 6 个 PR 完成。
2. 当某项“完全落地”后，需从本文件中删除该已完成项，避免形成历史堆积清单。
3. 删除已完成项之前，必须先通读相关代码并确认“实现完整 + 行为可用 + 验收通过”，不得仅凭提交记录判断。
4. 若仅部分实现，必须保留该项并拆分出剩余子项，禁止误删。

---

## PR-1：最小可用链路（单表增量 + 检查点）

- [ ] 落地 `SyncTableDefinition`、`SyncWindow`、`SyncCheckpoint`、`SyncBatchResult` 等领域模型。
- [ ] 落地 `ISyncOrchestrator`、`ISyncWindowCalculator`、`ISyncExecutionService` 及实现。
- [ ] 落地 `ISyncTaskConfigRepository`、`IOracleSourceReader`、`ISyncStagingRepository`、`ISyncUpsertRepository` 基础实现。
- [ ] 支持 `CursorColumn + StartTimeLocal` 增量窗口计算与分页读取。
- [ ] 支持 `UniqueKeys` 幂等合并（插入 / 覆盖更新 / 一致跳过）。
- [ ] 落地 `SyncCheckpoint` 持久化与失败后续跑。

## PR-2：审计与批次治理（可恢复可追踪）

- [ ] 落地 `ISyncBatchRepository`、`ISyncChangeLogRepository` 及实现。
- [ ] 打通批次状态流转：`Pending -> InProgress -> Completed/Failed`。
- [ ] 补齐 `ParentBatchId` 重试链路与失败重试关联追踪。
- [ ] 确保“读取、合并、日志写入全部成功后再提交检查点”的原子边界。
- [ ] 全链路异常日志接入 NLog（含 `TableCode`、`BatchId`、`Window`、`Checkpoint` 关键字段）。

## PR-3：删除同步（识别 + 执行 + 记录）

- [ ] 落地 `IDeletionExecutionService`、`ISyncDeletionRepository`、`ISyncDeletionLogRepository` 及实现。
- [ ] 实现源端存在性差异识别删除键（支持窗口内对比）。
- [ ] 实现 `DeletionPolicy`：`Disabled` / `SoftDelete` / `HardDelete`。
- [ ] 删除动作强制写入 `SyncDeletionLog` 与 `SyncChangeLog`。
- [ ] 支持删除 `DryRun`（仅审计不执行）并区分执行标记。

## PR-4：配置治理与准实时控制

- [ ] 完成 `SyncJobOptions`、`SyncTableOptions` 配置绑定与校验。
- [ ] 支持 `PollingIntervalSeconds`、`MaxLagMinutes` 调度与窗口控制。
- [ ] 支持 `Sync.ExcludedColumns` 并在读取 / 暂存 / 合并阶段统一生效。
- [ ] 增加排除列关键约束校验（禁止包含 `UniqueKeys`、`CursorColumn`、软删除标记列等）。
- [ ] 增加高优先级与低优先级表差异化调度参数。

## PR-5：分表保留期治理与危险动作门禁

- [ ] 落地 `IRetentionExecutionService`、`IShardTableResolver`、`IShardRetentionRepository` 及实现。
- [ ] 实现按分表时间维度保留期清理（如仅保留最近 3 个月）。
- [ ] 危险动作门禁落地：总开关、表级开关、dry-run、审计、回滚脚本。
- [ ] 落地 `RetentionBackgroundWorker` 定时任务。
- [ ] 补齐过期表识别、删除执行、失败回滚与审计闭环。

## PR-6：性能优化与验收收口

- [ ] 落地大表删除优化：键集分段 + 并行比对（`CompareSegmentSize`、`CompareMaxParallelism`）。
- [ ] 增加多表并发上限、指数退避重试、连续失败熔断策略。
- [ ] 增加同步指标与可观测性：延迟、积压、吞吐、失败率。
- [ ] 完成《Oracle到SQLServer同步架构设计.md》验收清单逐项验证。
- [ ] 全量通读代码，确认完全落地后删除本计划中已完成项。

---

## 计划来源映射

- 来源文档：`./Oracle到SQLServer同步架构设计.md`
- 对应章节：分层与命名、核心配置模型、同步流程、保留期清理、高效与实时性、分阶段落地建议、验收清单。
