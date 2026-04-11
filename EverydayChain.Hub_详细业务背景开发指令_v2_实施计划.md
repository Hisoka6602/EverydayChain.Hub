# EverydayChain.Hub 详细业务背景开发指令 v2 实施计划（多 PR 防跑偏版）

> 依据文档：`EverydayChain.Hub_详细业务背景开发指令_v2.md`
> 
> 目标：将“自动同步底座 + 外部 API 驱动业务链路”落地为可分阶段执行、可验收、可回滚的实施路径，避免多 PR 串行执行后目标漂移。

---

## 1. 总目标与边界

### 1.1 总目标
- 构建统一业务主线：**后台自动同步接单 -> 生成本地业务任务 -> 扫描上传 API -> 请求格口 API -> 落格回传 API -> 本地状态更新 -> 业务回传/补偿**。
- 在保留现有同步底座能力（KeyedMerge/StatusDriven、检查点、日志、删除治理、保留期治理）的前提下，新增业务域与对外 API 能力。

### 1.2 强边界（每个 PR 必检）
- 不新增 UI、看板、报表页面。
- 不重写现有同步宿主与同步编排。
- 不新增平行同步入口、平行 Oracle 读取器、平行 Merge 仓储、平行检查点机制。
- Controller 仅做入参校验与应用服务调用，不承载业务规则。
- 同步层自动回写（如 `TASKPROCESS`、`OPENTIME`）与业务回传必须严格分离。

### 1.3 基线复用清单（禁止重复实现）
- 宿主与后台：`Program.cs`、`SyncBackgroundWorker`、`RetentionBackgroundWorker`。
- 同步编排：`ISyncOrchestrator`、`SyncOrchestrator`、`ISyncExecutionService`、`SyncExecutionService`。
- 读取与落地：`OracleSourceReader`、`OracleStatusDrivenSourceReader`、`SyncStagingRepository`、`SqlServerSyncUpsertRepository`、`SqlServerAppendOnlyWriter`。
- 同步回写：`OracleRemoteStatusWriter`、`RemoteStatusConsumeService`。
- 配置与审计：`SyncTaskConfigRepository`、`SyncCheckpointRepository`、`InMemorySync*LogRepository`。

---

## 2. 交付治理模型（防跑偏机制）

### 2.1 PR 切分原则
- 每个 PR 只完成一个“可独立验收”的子目标。
- 每个 PR 必须同时包含：代码改动、对应测试、文档更新（含 `README.md` 联动）。
- 每个 PR 需明确“不在本 PR 范围”的事项，防止扩散。

### 2.2 依赖顺序（不可打乱）
1. 统一业务任务模型
2. 业务语义与接口基线文档
3. Host API 承载能力与契约模型
4. 条码解析与扫描输入模型
5. 扫描匹配/任务执行/格口解析
6. 落格回传
7. 业务回传
8. 扫描/落格日志
9. 异常规则（波次清理、多标签、回流）
10. 补偿机制

### 2.3 每个 PR 的固定验收模板
- 业务目标是否只覆盖当前阶段。
- 是否复用既有同步底座。
- 是否新增了不允许的平行实现。
- 是否满足分层放置与命名规范。
- 是否包含中文注释、中文异常、Swagger 中文说明（若涉及 API）。
- 是否补齐测试（单元/集成范围按当前阶段定义）。
- 是否更新 `README.md` 文件树与“逐项作用说明”。
- 是否给出待确认项与未决风险。

### 2.4 需求不明确治理
- 发现不明确项时，必须在 PR 描述维护“待确认项”清单。
- 待确认项未关闭前，不做默认实现。

---

## 3. 分阶段实施计划（按 PR 执行）

## 阶段 A（P0）：统一业务任务模型

### A.1 本阶段目标
- 引入 API 链路统一主实体，解耦“同步原始表”与“业务执行对象”。

### A.2 计划新增
- `EverydayChain.Hub.Domain/BusinessTasks/BusinessTaskEntity.cs`
- `EverydayChain.Hub.Domain/BusinessTasks/BusinessTaskStatus.cs`
- `EverydayChain.Hub.Domain/BusinessTasks/BusinessTaskType.cs`
- `EverydayChain.Hub.Domain/BusinessTasks/BusinessTaskSourceType.cs`
- `EverydayChain.Hub.Application/BusinessTasks/Abstractions/IBusinessTaskMaterializer.cs`
- `EverydayChain.Hub.Application/BusinessTasks/Services/BusinessTaskMaterializer.cs`

### A.3 验收口径
- 统一业务任务实体可覆盖拆零/整件基础字段。
- 转换服务只做映射与默认状态赋值，不承载扫描/格口/落格业务。
- 不破坏既有同步任务执行与检查点机制。

---

## 阶段 B（P0）：语义基线文档固化

### B.1 本阶段目标
- 固化跨 PR 不可变业务语义，避免术语漂移。

### B.2 计划新增
- `WMS状态语义基线.md`
- `条码规则基线.md`
- `对外API接口基线.md`
- `拆零业务字段语义基线.md`
- `整件业务字段语义基线.md`

### B.3 验收口径
- 明确读取状态、回传状态、同步自动回写字段、业务回传字段边界。
- 三个对外 API 的路由、方法、入参、出参、幂等语义、状态迁移完整可查。

---

## 阶段 C（P1）：Host API 承载与接口契约

### C.1 本阶段目标
- 在保留 Worker 的同时启用 Web API 承载能力。

### C.2 计划新增
- `EverydayChain.Hub.Host/Controllers/ScanController.cs`
- `EverydayChain.Hub.Host/Controllers/ChuteController.cs`
- `EverydayChain.Hub.Host/Controllers/DropFeedbackController.cs`
- `EverydayChain.Hub.Host/Contracts/Requests/*.cs`
- `EverydayChain.Hub.Host/Contracts/Responses/*.cs`

### C.3 验收口径
- API 与后台 Worker 共存运行。
- Controller 不含业务规则。
- Swagger 参数/方法/枚举中文说明齐全。

---

## 阶段 D（P1）：扫描输入模型与条码解析

### D.1 本阶段目标
- 建立统一扫描事件输入与条码解析能力。

### D.2 计划新增
- `EverydayChain.Hub.Domain/Barcodes/BarcodeType.cs`
- `EverydayChain.Hub.Domain/Barcodes/BarcodeParseResult.cs`
- `EverydayChain.Hub.Application/ScanProcessing/Models/ScanEventArgs.cs`
- `EverydayChain.Hub.Application/ScanProcessing/Models/ScanMeasurementInfo.cs`
- `EverydayChain.Hub.Application/Barcodes/Abstractions/IBarcodeParser.cs`
- `EverydayChain.Hub.Application/Barcodes/Services/BarcodeParser.cs`

### D.3 验收口径
- 可完成条码类型识别、无效码过滤、拆零/整件解析。
- 输出统一解析结果，供后续扫描匹配使用。

---

## 阶段 E（P1）：扫描匹配、格口请求、任务执行

### E.1 本阶段目标
- 打通“扫描上传”和“请求格口”核心链路。

### E.2 计划新增
- `EverydayChain.Hub.Application/ScanProcessing/Abstractions/IScanMatchService.cs`
- `EverydayChain.Hub.Application/ScanProcessing/Models/ScanMatchResult.cs`
- `EverydayChain.Hub.Application/ScanProcessing/Services/ScanMatchService.cs`
- `EverydayChain.Hub.Application/TaskExecution/Abstractions/ITaskExecutionService.cs`
- `EverydayChain.Hub.Application/TaskExecution/Services/TaskExecutionService.cs`
- `EverydayChain.Hub.Application/Chutes/Abstractions/IChuteResolveService.cs`
- `EverydayChain.Hub.Application/Chutes/Services/ChuteResolveService.cs`
- `EverydayChain.Hub.Application/Chutes/Models/ChuteResolveResult.cs`

### E.3 验收口径
- 扫描匹配、状态更新、扫描日志写入、格口解析职责分离。
- “请求格口”只返回格口结果，不确认落格。

---

## 阶段 F（P2）：落格回传

### F.1 本阶段目标
- 打通“真实落格完成后”状态变更链路。

### F.2 计划新增
- `EverydayChain.Hub.Application/DropFeedback/Abstractions/IDropFeedbackService.cs`
- `EverydayChain.Hub.Application/DropFeedback/Services/DropFeedbackService.cs`
- `EverydayChain.Hub.Domain/DropFeedback/DropFeedbackResult.cs`

### F.3 验收口径
- 可按任务标识更新 Dropped/Exception 状态。
- 可更新落格时间、失败原因与后续回传前置状态。

---

## 阶段 G（P2）：业务回传（非同步自动回写）

### G.1 本阶段目标
- 实现“落格后业务结果回传”独立链路。

### G.2 计划新增
- `EverydayChain.Hub.Application/Feedback/Abstractions/IWmsFeedbackService.cs`
- `EverydayChain.Hub.Application/Feedback/Services/WmsFeedbackService.cs`
- `EverydayChain.Hub.Infrastructure/Feedback/OracleWmsFeedbackWriter.cs`

### G.3 验收口径
- 严格区分同步自动回写与业务回传。
- 仅对已落格待回传任务执行回写并回填本地回传状态。

---

## 阶段 H（P1/P2）：扫描日志与落格日志

### H.1 本阶段目标
- 建立扫描与落格全链路审计能力。

### H.2 计划新增
- `EverydayChain.Hub.Domain/Aggregates/ScanLogAggregate/ScanLogEntity.cs`
- `EverydayChain.Hub.Domain/Aggregates/DropLogAggregate/DropLogEntity.cs`
- `EverydayChain.Hub.Infrastructure/Persistence/EntityConfigurations/ScanLogEntityTypeConfiguration.cs`
- `EverydayChain.Hub.Infrastructure/Persistence/EntityConfigurations/DropLogEntityTypeConfiguration.cs`

### H.3 验收口径
- 扫描成功/失败均记录。
- 落格成功/失败均记录。

---

## 阶段 I（P2）：异常规则

### I.1 本阶段目标
- 补齐波次清理、多标签、回流决策。

### I.2 计划新增
- `Application/WaveCleanup/Abstractions/IWaveCleanupService.cs`
- `Application/WaveCleanup/Services/WaveCleanupService.cs`
- `Domain/MultiLabel/MultiLabelDecisionResult.cs`
- `Application/MultiLabel/Abstractions/IMultiLabelDecisionService.cs`
- `Application/MultiLabel/Services/MultiLabelDecisionService.cs`
- `Domain/Recirculation/RecirculationDecisionResult.cs`
- `Application/Recirculation/Services/RecirculationService.cs`

### I.3 验收口径
- 三类异常规则可配置、可审计、可追踪。
- 关键动作支持 dry-run 与审计日志。

---

## 阶段 J（P2）：补偿机制

### J.1 本阶段目标
- 对业务回传失败任务提供重试闭环。

### J.2 计划新增
- `EverydayChain.Hub.Application/Compensation/Abstractions/ICompensationService.cs`
- `EverydayChain.Hub.Application/Compensation/Services/CompensationService.cs`

### J.3 验收口径
- 支持按任务重试、按批次重试。
- 补偿操作具备日志审计与可观测输出。

---

## 4. 多 PR 协同执行规则

### 4.1 分支与合并规则
- 每阶段一个独立 PR，按 A->J 顺序合并。
- 禁止跨阶段混改；若确需跨阶段，必须在 PR 标题显式标注“跨阶段例外”。

### 4.2 漂移拦截点（每次评审必查）
- 是否引入了与阶段目标无关的目录或抽象。
- 是否出现“同职责双实现”或“一层透传服务”。
- 是否把业务规则写入 Controller 或 Infrastructure 协议层。
- 是否修改了已确认语义（状态枚举、接口契约）且未同步基线文档。

### 4.3 统一验收清单（每 PR 附带）
- [ ] 阶段目标与本 PR 范围一致
- [ ] 现有同步底座已复用，未重复造轮子
- [ ] 分层目录与命名符合 DDD 约束
- [ ] 测试通过（`dotnet build`、`dotnet test`）
- [ ] API/配置/枚举中文说明完整（涉及时）
- [ ] `README.md` 已联动更新
- [ ] 待确认项已记录并可追踪

---

## 5. 关键风险与应对

- 风险 1：同步自动回写与业务回传混淆。
  - 应对：阶段 G 前禁止新增业务回传写库逻辑；通过基线文档与评审清单双重约束。
- 风险 2：Controller 业务膨胀。
  - 应对：Controller 只保留参数校验与应用服务调用，业务规则统一下沉 Application。
- 风险 3：多 PR 引入重复抽象。
  - 应对：新增接口前强制检索同职责抽象，重复命名直接驳回。
- 风险 4：业务状态语义漂移。
  - 应对：状态枚举与接口语义变更必须先更新阶段 B 基线文档并在 PR 显式说明。

---

## 6. 待确认项（启动实施前需明确）

1. `BusinessTaskEntity` 与现有聚合实体的最终映射关系及主键生成策略。
2. 三个对外 API 的鉴权方式、调用方身份透传字段与审计字段最小集合。
3. 业务回传（阶段 G）目标 Oracle 表与字段映射清单、失败重试上限、幂等键。
4. 异常规则（阶段 I）中“多标签”“回流”的判定优先级冲突策略。
5. 补偿（阶段 J）任务调度模式（即时触发/定时批处理）与告警阈值。

> 上述待确认项未关闭前，仅允许完成不依赖该决策的代码与文档。
