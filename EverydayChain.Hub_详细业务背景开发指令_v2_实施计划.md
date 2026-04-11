# EverydayChain.Hub 详细业务背景开发指令 v2 实施计划（PR 级执行版）

> 依据：`EverydayChain.Hub_详细业务背景开发指令_v2.md`
> 
> 目标：提供可直接拆分为多个 PR 的执行蓝图；每个 PR 明确“做什么、改多少文件、交付什么、如何验收”，避免串行交付跑偏。

---

## 1. 全局边界与统一口径

### 1.1 统一业务主链路
后台自动同步接单 -> 生成本地业务任务 -> 扫描上传 API -> 请求格口 API -> 落格回传 API -> 本地状态更新 -> 业务回传/补偿。

### 1.2 不可突破边界（所有 PR 强制）
- 不做 UI，不做看板，不做报表页面。
- 不重写同步底座（`SyncOrchestrator`、`SyncExecutionService`、`OracleSourceReader`、`SqlServerSyncUpsertRepository`、`RemoteStatusConsumeService` 等）。
- 不新增平行同步总入口、平行检查点、平行调度器。
- Controller 只做入参校验与调用 Application 服务。
- “同步自动回写（TASKPROCESS/OPENTIME）”与“业务回传”必须分离。

### 1.3 全局验收门禁（每个 PR 都要满足）
- `dotnet build /home/runner/work/EverydayChain.Hub/EverydayChain.Hub/EverydayChain.Hub.sln`
- `dotnet test /home/runner/work/EverydayChain.Hub/EverydayChain.Hub/EverydayChain.Hub.sln`
- README 联动更新（涉及新增/删除文件时必须更新文件树与逐项说明）。
- PR 描述必须包含：范围、交付件、验收结果、未决待确认项。

---

## 2. PR 切分总览（执行顺序）

| PR | 主题 | 依赖 | 预计改动文件数 | 交付结果摘要 |
|---|---|---|---|---|
| PR-01 | 统一业务任务模型与转换服务 | 无 | 新增 8，修改 4，合计约 12 | 建立业务任务主实体与转换入口 |
| PR-02 | 业务语义与接口基线文档 | PR-01 | 新增 5，修改 2，合计约 7 | 固化状态语义/条码规则/API 基线 |
| PR-03 | Host API 承载骨架（含契约） | PR-02 | 新增 12，修改 4，合计约 16 | 打通 API 宿主，保留 Worker 共存 |
| PR-04 | 条码解析与扫描输入模型 | PR-03 | 新增 8，修改 3，合计约 11 | 提供标准化条码解析结果 |
| PR-05 | 扫描匹配与任务执行服务 | PR-04 | 新增 10，修改 6，合计约 16 | 扫描上传业务链路可用 |
| PR-06 | 请求格口服务 | PR-05 | 新增 4，修改 4，合计约 8 | 请求格口接口可返回目标格口 |
| PR-07 | 落格回传服务 | PR-06 | 新增 4，修改 5，合计约 9 | 落格回传后更新业务状态 |
| PR-08 | 业务回传服务（非同步自动回写） | PR-07 | 新增 4，修改 6，合计约 10 | 已落格任务可执行业务回传 |
| PR-09 | 扫描日志与落格日志实体落库 | PR-07 | 新增 6，修改 5，合计约 11 | 扫描/落格成功失败均有审计 |
| PR-10 | 异常规则（波次/多标签/回流） | PR-07 | 新增 8，修改 5，合计约 13 | 异常决策链路可配置可审计 |
| PR-11 | 补偿服务 | PR-08,PR-10 | 新增 3，修改 5，合计约 8 | 回传失败任务支持重试闭环 |
| PR-12 | 稳定化收口与联调验收 | PR-03~PR-11 | 新增 0~2，修改 6~10，合计约 8~12 | 全链路联调、文档收口、验收归档 |

> 说明：文件数为实施计划值，允许 ±2 浮动；超出需在 PR 描述写明原因。

---

## 3. PR 级执行明细

## PR-01：统一业务任务模型与转换服务（P0）

### 3.1 预计改动文件
- **新增（8）**
  1. `EverydayChain.Hub.Domain/BusinessTasks/BusinessTaskEntity.cs`
  2. `EverydayChain.Hub.Domain/BusinessTasks/BusinessTaskStatus.cs`
  3. `EverydayChain.Hub.Domain/BusinessTasks/BusinessTaskType.cs`
  4. `EverydayChain.Hub.Domain/BusinessTasks/BusinessTaskSourceType.cs`
  5. `EverydayChain.Hub.Application/BusinessTasks/Abstractions/IBusinessTaskMaterializer.cs`
  6. `EverydayChain.Hub.Application/BusinessTasks/Services/BusinessTaskMaterializer.cs`
  7. `EverydayChain.Hub.Tests/BusinessTasks/BusinessTaskMaterializerTests.cs`
  8. `EverydayChain.Hub.Tests/BusinessTasks/BusinessTaskEntityValidationTests.cs`
- **修改（4）**
  - `EverydayChain.Hub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
  - `EverydayChain.Hub.Application/EverydayChain.Hub.Application.csproj`
  - `EverydayChain.Hub.Domain/EverydayChain.Hub.Domain.csproj`
  - `README.md`

### 3.2 执行步骤
1. 建立 `BusinessTasks` 领域目录与实体/枚举。
2. 实现 `BusinessTaskMaterializer`（仅映射与默认状态赋值）。
3. 注入 DI。
4. 增加单元测试覆盖拆零/整件映射、默认状态、空值保护。
5. 联动 README。

### 3.3 交付结果
- 形成“同步记录 -> 业务任务”的统一入口，后续 API 不再直接依赖同步原始表语义。

### 3.4 验收标准
- `BusinessTaskStatus` 覆盖 Created/Scanned/Dropped/FeedbackPending 等状态。
- `BusinessTaskMaterializer` 不出现扫描、格口、落格业务逻辑。
- 单元测试通过且覆盖关键字段映射。

---

## PR-02：业务语义与接口基线文档（P0）

### 3.5 预计改动文件
- **新增（5）**
  1. `WMS状态语义基线.md`
  2. `条码规则基线.md`
  3. `对外API接口基线.md`
  4. `拆零业务字段语义基线.md`
  5. `整件业务字段语义基线.md`
- **修改（2）**
  - `EverydayChain.Hub_详细业务背景开发指令_v2_实施计划.md`
  - `README.md`

### 3.6 执行步骤
1. 将状态语义、条码语义、接口语义固化为基线文档。
2. 对每个 API 补齐路由、方法、入参、出参、幂等语义、状态变化。
3. 在实施计划中标注这些文档作为后续 PR 约束依据。

### 3.7 交付结果
- 后续所有 PR 都有统一语义锚点，减少实现分叉。

### 3.8 验收标准
- 文档之间术语一致（TASKPROCESS/STATUS/Drop/Feedback）。
- 三个 API 均有成功失败语义与幂等规则。

---

## PR-03：Host API 承载骨架（P1）

### 3.9 预计改动文件
- **新增（12）**
  - `EverydayChain.Hub.Host/Controllers/ScanController.cs`
  - `EverydayChain.Hub.Host/Controllers/ChuteController.cs`
  - `EverydayChain.Hub.Host/Controllers/DropFeedbackController.cs`
  - `EverydayChain.Hub.Host/Contracts/Requests/ScanUploadRequest.cs`
  - `EverydayChain.Hub.Host/Contracts/Requests/ChuteRequestRequest.cs`
  - `EverydayChain.Hub.Host/Contracts/Requests/DropFeedbackRequest.cs`
  - `EverydayChain.Hub.Host/Contracts/Responses/ScanUploadResponse.cs`
  - `EverydayChain.Hub.Host/Contracts/Responses/ChuteRequestResponse.cs`
  - `EverydayChain.Hub.Host/Contracts/Responses/DropFeedbackResponse.cs`
  - `EverydayChain.Hub.Tests/Host/Controllers/ScanControllerTests.cs`
  - `EverydayChain.Hub.Tests/Host/Controllers/ChuteControllerTests.cs`
  - `EverydayChain.Hub.Tests/Host/Controllers/DropFeedbackControllerTests.cs`
- **修改（4）**
  - `EverydayChain.Hub.Host/Program.cs`
  - `EverydayChain.Hub.Host/EverydayChain.Hub.Host.csproj`
  - `EverydayChain.Hub.Host/appsettings.json`
  - `README.md`

### 3.10 执行步骤
1. 在 Host 中启用 Controllers/Swagger。
2. 保留现有 Worker 注册，确保 API 与 Worker 共存。
3. 加入请求/响应契约，补齐中文注释。
4. 完成控制器最小行为测试。

### 3.11 交付结果
- 对外 HTTP 接口承载完成，具备后续业务接线点。

### 3.12 验收标准
- 启动后 Worker 与 API 同时运行。
- Controller 不含业务规则，只有入参校验与服务调用。
- Swagger 可见中文参数说明。

---

## PR-04：条码解析与扫描输入模型（P1）

### 3.13 预计改动文件
- **新增（8）**
  - `EverydayChain.Hub.Domain/Barcodes/BarcodeType.cs`
  - `EverydayChain.Hub.Domain/Barcodes/BarcodeParseResult.cs`
  - `EverydayChain.Hub.Application/ScanProcessing/Models/ScanEventArgs.cs`
  - `EverydayChain.Hub.Application/ScanProcessing/Models/ScanMeasurementInfo.cs`
  - `EverydayChain.Hub.Application/Barcodes/Abstractions/IBarcodeParser.cs`
  - `EverydayChain.Hub.Application/Barcodes/Services/BarcodeParser.cs`
  - `EverydayChain.Hub.Tests/Barcodes/BarcodeParserTests.cs`
  - `EverydayChain.Hub.Tests/ScanProcessing/ScanEventArgsTests.cs`
- **修改（3）**
  - `EverydayChain.Hub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
  - `EverydayChain.Hub.Application/EverydayChain.Hub.Application.csproj`
  - `README.md`

### 3.14 执行步骤
1. 定义条码类型枚举与解析结果模型。
2. 实现解析服务（类型识别、无效码过滤、结构提取）。
3. 定义扫描输入模型（`record class`）。
4. 增加解析规则测试。

### 3.15 交付结果
- 扫描上传链路有可复用的标准化条码解析能力。

### 3.16 验收标准
- 解析服务可区分拆零/整件/无效码。
- `ScanEventArgs` 与测量信息模型可覆盖 API 输入。

---

## PR-05：扫描匹配与任务执行（P1）

### 3.17 预计改动文件
- **新增（10）**
  - `EverydayChain.Hub.Application/ScanProcessing/Abstractions/IScanMatchService.cs`
  - `EverydayChain.Hub.Application/ScanProcessing/Models/ScanMatchResult.cs`
  - `EverydayChain.Hub.Application/ScanProcessing/Services/ScanMatchService.cs`
  - `EverydayChain.Hub.Application/TaskExecution/Abstractions/ITaskExecutionService.cs`
  - `EverydayChain.Hub.Application/TaskExecution/Services/TaskExecutionService.cs`
  - `EverydayChain.Hub.Application/Abstractions/Persistence/IBusinessTaskRepository.cs`
  - `EverydayChain.Hub.Infrastructure/Repositories/BusinessTaskRepository.cs`
  - `EverydayChain.Hub.Tests/ScanProcessing/ScanMatchServiceTests.cs`
  - `EverydayChain.Hub.Tests/TaskExecution/TaskExecutionServiceTests.cs`
  - `EverydayChain.Hub.Tests/Repositories/BusinessTaskRepositoryTests.cs`
- **修改（6）**
  - `EverydayChain.Hub.Host/Controllers/ScanController.cs`
  - `EverydayChain.Hub.Infrastructure/Persistence/HubDbContext.cs`
  - `EverydayChain.Hub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
  - `EverydayChain.Hub.Infrastructure/Migrations/*`（新增迁移文件）
  - `EverydayChain.Hub.Host/appsettings.json`
  - `README.md`

### 3.18 执行步骤
1. 建立扫描匹配与任务执行服务。
2. 增加业务任务仓储抽象与实现。
3. 扫描接口接入解析 -> 匹配 -> 执行。
4. 增加 EF 映射与迁移。

### 3.19 交付结果
- 扫描上传可真正更新业务任务状态、计数、测量信息。

### 3.20 验收标准
- 匹配成功/失败都产生明确结果。
- 扫描次数、扫描时间、状态迁移正确。
- 数据库迁移可应用且不破坏既有表。

---

## PR-06：请求格口服务（P1）

### 3.21 预计改动文件
- **新增（4）**
  - `EverydayChain.Hub.Application/Chutes/Abstractions/IChuteResolveService.cs`
  - `EverydayChain.Hub.Application/Chutes/Services/ChuteResolveService.cs`
  - `EverydayChain.Hub.Application/Chutes/Models/ChuteResolveResult.cs`
  - `EverydayChain.Hub.Tests/Chutes/ChuteResolveServiceTests.cs`
- **修改（4）**
  - `EverydayChain.Hub.Host/Controllers/ChuteController.cs`
  - `EverydayChain.Hub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
  - `EverydayChain.Hub.Host/Contracts/Responses/ChuteRequestResponse.cs`
  - `README.md`

### 3.22 执行步骤
1. 实现格口解析服务。
2. 将请求格口 API 接入该服务。
3. 补充无任务/异常任务分支测试。

### 3.23 交付结果
- 请求格口接口可依据本地任务返回目标格口与异常原因。

### 3.24 验收标准
- 返回结果包含 `IsSuccess/TargetChuteCode/TaskId/FailureReason`。
- 不写入“已落格”状态。

---

## PR-07：落格回传服务（P2）

### 3.25 预计改动文件
- **新增（4）**
  - `EverydayChain.Hub.Application/DropFeedback/Abstractions/IDropFeedbackService.cs`
  - `EverydayChain.Hub.Application/DropFeedback/Services/DropFeedbackService.cs`
  - `EverydayChain.Hub.Domain/DropFeedback/DropFeedbackResult.cs`
  - `EverydayChain.Hub.Tests/DropFeedback/DropFeedbackServiceTests.cs`
- **修改（5）**
  - `EverydayChain.Hub.Host/Controllers/DropFeedbackController.cs`
  - `EverydayChain.Hub.Domain/BusinessTasks/BusinessTaskStatus.cs`
  - `EverydayChain.Hub.Infrastructure/Repositories/BusinessTaskRepository.cs`
  - `EverydayChain.Hub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
  - `README.md`

### 3.26 执行步骤
1. 实现落格回传服务。
2. 接口接入 `TaskId/Barcode` 双路径定位任务。
3. 更新落格状态、落格时间、失败原因。

### 3.27 交付结果
- 外部“真实落格完成”后，本地任务状态可正确推进。

### 3.28 验收标准
- 成功回传进入 `Dropped`，失败回传进入 `Exception`。
- 回传后可进入待业务回传状态。

---

## PR-08：业务回传服务（P2）

### 3.29 预计改动文件
- **新增（4）**
  - `EverydayChain.Hub.Application/Feedback/Abstractions/IWmsFeedbackService.cs`
  - `EverydayChain.Hub.Application/Feedback/Services/WmsFeedbackService.cs`
  - `EverydayChain.Hub.Infrastructure/Feedback/OracleWmsFeedbackWriter.cs`
  - `EverydayChain.Hub.Tests/Feedback/WmsFeedbackServiceTests.cs`
- **修改（6）**
  - `EverydayChain.Hub.Application/BusinessTasks/BusinessTaskEntity.cs`
  - `EverydayChain.Hub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
  - `EverydayChain.Hub.Host/Program.cs`（注册后台回传任务时）
  - `EverydayChain.Hub.Host/appsettings.json`
  - `EverydayChain.Hub.Infrastructure/Repositories/BusinessTaskRepository.cs`
  - `README.md`

### 3.30 执行步骤
1. 查询已落格待回传任务。
2. 组装业务回传 payload。
3. 调用 Oracle 写入器回写。
4. 回填本地回传状态与时间。

### 3.31 交付结果
- 形成“落格完成 -> 业务回传 -> 本地回填”的闭环。

### 3.32 验收标准
- 不复用/不改写同步层自动回写链路。
- 仅处理待回传任务，具备幂等保护。

---

## PR-09：扫描日志与落格日志（P1/P2）

### 3.33 预计改动文件
- **新增（6）**
  - `EverydayChain.Hub.Domain/Aggregates/ScanLogAggregate/ScanLogEntity.cs`
  - `EverydayChain.Hub.Domain/Aggregates/DropLogAggregate/DropLogEntity.cs`
  - `EverydayChain.Hub.Infrastructure/Persistence/EntityConfigurations/ScanLogEntityTypeConfiguration.cs`
  - `EverydayChain.Hub.Infrastructure/Persistence/EntityConfigurations/DropLogEntityTypeConfiguration.cs`
  - `EverydayChain.Hub.Tests/Logs/ScanLogPersistenceTests.cs`
  - `EverydayChain.Hub.Tests/Logs/DropLogPersistenceTests.cs`
- **修改（5）**
  - `EverydayChain.Hub.Infrastructure/Persistence/HubDbContext.cs`
  - `EverydayChain.Hub.Infrastructure/Migrations/*`（新增迁移文件）
  - `EverydayChain.Hub.Application/TaskExecution/Services/TaskExecutionService.cs`
  - `EverydayChain.Hub.Application/DropFeedback/Services/DropFeedbackService.cs`
  - `README.md`

### 3.34 执行步骤
1. 新增日志聚合实体与 EF 映射。
2. 扫描/落格服务接入日志写入。
3. 增加迁移与持久化测试。

### 3.35 交付结果
- 扫描、落格全链路有业务审计数据。

### 3.36 验收标准
- 成功与失败都产生日志记录。
- 日志字段含任务标识、时间、结果、原因。

---

## PR-10：异常规则（P2）

### 3.37 预计改动文件
- **新增（8）**
  - `EverydayChain.Hub.Application/WaveCleanup/Abstractions/IWaveCleanupService.cs`
  - `EverydayChain.Hub.Application/WaveCleanup/Services/WaveCleanupService.cs`
  - `EverydayChain.Hub.Domain/MultiLabel/MultiLabelDecisionResult.cs`
  - `EverydayChain.Hub.Application/MultiLabel/Abstractions/IMultiLabelDecisionService.cs`
  - `EverydayChain.Hub.Application/MultiLabel/Services/MultiLabelDecisionService.cs`
  - `EverydayChain.Hub.Domain/Recirculation/RecirculationDecisionResult.cs`
  - `EverydayChain.Hub.Application/Recirculation/Services/RecirculationService.cs`
  - `EverydayChain.Hub.Tests/Rules/ExceptionRulesTests.cs`
- **修改（5）**
  - `EverydayChain.Hub.Domain/BusinessTasks/BusinessTaskEntity.cs`
  - `EverydayChain.Hub.Application/TaskExecution/Services/TaskExecutionService.cs`
  - `EverydayChain.Hub.Host/appsettings.json`
  - `EverydayChain.Hub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
  - `README.md`

### 3.38 执行步骤
1. 实现波次清理、多标签、回流规则服务。
2. 在任务执行链路挂接规则判定。
3. 补充 dry-run 与日志审计路径。

### 3.39 交付结果
- 异常分支可统一决策、可追踪、可审计。

### 3.40 验收标准
- 三类规则都有可测试的输入输出。
- dry-run 不做破坏性写入。

---

## PR-11：补偿服务（P2）

### 3.41 预计改动文件
- **新增（3）**
  - `EverydayChain.Hub.Application/Compensation/Abstractions/ICompensationService.cs`
  - `EverydayChain.Hub.Application/Compensation/Services/CompensationService.cs`
  - `EverydayChain.Hub.Tests/Compensation/CompensationServiceTests.cs`
- **修改（5）**
  - `EverydayChain.Hub.Application/Feedback/Services/WmsFeedbackService.cs`
  - `EverydayChain.Hub.Infrastructure/Repositories/BusinessTaskRepository.cs`
  - `EverydayChain.Hub.Host/Program.cs`（后台补偿入口）
  - `EverydayChain.Hub.Host/appsettings.json`
  - `README.md`

### 3.42 执行步骤
1. 识别回传失败任务。
2. 支持按任务、按批次重试。
3. 重试结果落日志与状态回填。

### 3.43 交付结果
- 业务回传失败有自动/半自动补偿通道。

### 3.44 验收标准
- 补偿可重入、可追踪、可配置限流。
- 重试成功后状态正确关闭，失败保留可重试标识。

---

## PR-12：稳定化收口与联调验收（P0）

### 3.45 预计改动文件
- **新增（0~2）**
  - 可能新增联调清单或回归报告文档
- **修改（6~10）**
  - `README.md`
  - `EverydayChain.Hub_详细业务背景开发指令_v2_实施计划.md`
  - `对外API接口基线.md`
  - `WMS状态语义基线.md`
  - `EverydayChain.Hub.Host/appsettings.json`
  - `EverydayChain.Hub.Tests/*`（补齐联调回归）

### 3.46 执行步骤
1. 执行全链路联调（扫描 -> 格口 -> 落格 -> 回传 -> 补偿）。
2. 统一清理跨 PR 术语与文档偏差。
3. 收敛测试缺口，补齐回归用例。
4. 出具最终验收清单。

### 3.47 交付结果
- 可对外说明“已实现能力、未实现能力、边界与后续计划”。

### 3.48 验收标准
- 所有核心链路可跑通。
- 构建测试全绿。
- 文档、代码、配置三者一致。

---

## 4. 每个 PR 必填交付模板（直接复制到 PR 描述）

```md
## 本 PR 范围
- 目标：
- 不在范围：

## 改动文件统计
- 新增：X 个
- 修改：Y 个
- 删除：Z 个
- 合计：X+Y+Z 个

## 主要交付结果
1. 
2. 
3. 

## 验收结果
- [ ] 构建通过
- [ ] 测试通过
- [ ] 业务验收点 1
- [ ] 业务验收点 2
- [ ] 未引入平行同步实现
- [ ] README 与基线文档已联动

## 待确认项
1. 
2. 

## 风险与回滚
- 风险：
- 回滚方式：
```

---

## 5. 关键待确认项（不确认不实施）

1. 业务任务主键生成与外部幂等键（是否统一使用 `TaskId + TraceId`）。
2. 三个 API 的鉴权机制与调用方身份透传字段。
3. 业务回传 Oracle 目标表、字段、失败码语义。
4. 多标签与回流冲突时的优先级规则。
5. 补偿策略的频率、上限与告警阈值。

> 规则：待确认项未关闭时，仅允许实施不依赖该决策的内容。
