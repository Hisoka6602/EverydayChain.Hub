# EverydayChain.Hub 详细业务背景开发指令 v2 实施计划（PR 排期执行版）

> 依据：`EverydayChain.Hub_详细业务背景开发指令_v2.md`
>
> 输出目标：将实施工作拆分为可直接排期的多个 PR；每个 PR 都明确业务逻辑链路、复用与参考文件、执行步骤、交付结果、验收标准、建议标题/分支名/评审关注点。
>
> 审查对齐：本计划文档与 `README.md` 同步维护，确保 PR 标题/描述与实际改动文件一致。

---

## 1. 全局业务链路（统一口径）

### 1.1 主链路
1. 同步底座将 Oracle 源记录拉取到本地。
2. 将同步记录物化为业务任务（BusinessTask）。
3. 设备调用“扫描上传 API”推进任务状态。
4. 设备调用“请求格口 API”获取目标格口。
5. 设备调用“落格回传 API”确认实际落格结果。
6. 系统执行业务回传（与同步自动回写严格分离）。
7. 回传失败进入补偿重试链路。

### 1.2 必须复用的既有底座文件（所有 PR 默认复用）
- `EverydayChain.Hub.Application/Abstractions/Services/ISyncOrchestrator.cs`
- `EverydayChain.Hub.Application/Services/SyncOrchestrator.cs`
- `EverydayChain.Hub.Application/Abstractions/Services/ISyncExecutionService.cs`
- `EverydayChain.Hub.Application/Services/SyncExecutionService.cs`
- `EverydayChain.Hub.Infrastructure/Repositories/OracleSourceReader.cs`
- `EverydayChain.Hub.Infrastructure/Sync/Readers/OracleStatusDrivenSourceReader.cs`
- `EverydayChain.Hub.Infrastructure/Repositories/SqlServerSyncUpsertRepository.cs`
- `EverydayChain.Hub.Infrastructure/Sync/Writers/SqlServerAppendOnlyWriter.cs`
- `EverydayChain.Hub.Infrastructure/Sync/Writers/OracleRemoteStatusWriter.cs`
- `EverydayChain.Hub.Infrastructure/Sync/Services/RemoteStatusConsumeService.cs`
- `EverydayChain.Hub.Infrastructure/Repositories/SyncTaskConfigRepository.cs`
- `EverydayChain.Hub.Host/Workers/SyncBackgroundWorker.cs`
- `EverydayChain.Hub.Host/Workers/RetentionBackgroundWorker.cs`
- `EverydayChain.Hub.Host/Program.cs`

### 1.3 全局实施边界
- 不新增平行同步入口、不重写同步编排。
- Controller 仅做参数校验与应用服务调用。
- 同步自动回写（状态消费）与业务回传（业务闭环）分离。
- 每个 PR 必须附带可执行验收项。

### 1.4 全局统一验收
- `dotnet build /home/runner/work/EverydayChain.Hub/EverydayChain.Hub/EverydayChain.Hub.sln`
- `dotnet test /home/runner/work/EverydayChain.Hub/EverydayChain.Hub/EverydayChain.Hub.sln`
- 若 PR 涉及新增/删除文件：联动更新 `README.md` 文件树与逐项说明。

---

## 2. PR 排期总览

| PR | 阶段 | 依赖 | 预计文件改动 | 建议分支名 |
|---|---|---|---|---|
| PR-01 | 统一业务任务模型 | 无 | 新增 8 / 修改 4 / 合计 12 | `feature/pr01-business-task-model` |
| PR-02 | 语义与接口基线文档 | PR-01 | 新增 5 / 修改 2 / 合计 7 | `feature/pr02-semantic-baseline` |
| PR-03 | Host API 承载骨架 | PR-02 | 新增 12 / 修改 4 / 合计 16 | `feature/pr03-host-api-bootstrap` |
| PR-04 | 条码解析与扫描输入 | PR-03 | 新增 8 / 修改 3 / 合计 11 | `feature/pr04-barcode-and-scan-model` |
| PR-05 | 扫描匹配与任务执行 | PR-04 | 新增 10 / 修改 6 / 合计 16 | `feature/pr05-scan-match-and-execution` |
| PR-06 | 请求格口服务 | PR-05 | 新增 4 / 修改 4 / 合计 8 | `feature/pr06-chute-resolve` |
| PR-07 | 落格回传服务 | PR-06 | 新增 4 / 修改 5 / 合计 9 | `feature/pr07-drop-feedback` |
| PR-08 | 业务回传服务 | PR-07 | 新增 4 / 修改 6 / 合计 10 | `feature/pr08-wms-feedback` |
| PR-09 | 扫描/落格日志落库 | PR-07 | 新增 6 / 修改 5 / 合计 11 | `feature/pr09-scan-drop-logs` |
| PR-10 | 异常规则链路 | PR-07 | 新增 8 / 修改 5 / 合计 13 | `feature/pr10-exception-rules` |
| PR-11 | 补偿重试链路 | PR-08,PR-10 | 新增 3 / 修改 5 / 合计 8 | `feature/pr11-compensation` |
| PR-12 | 联调收口与验收归档 | PR-03~PR-11 | 新增 0~2 / 修改 6~10 / 合计 8~12 | `feature/pr12-stabilization` |

> 说明：预计改动为排期预算，允许 ±2 文件浮动；超出需在 PR 描述说明原因。

---

## 3. 每个 PR 的执行清单（可直接用于排期）

## PR-01：统一业务任务模型

### 建议标题
`feat(domain): 新增业务任务主模型与物化服务`

### 业务逻辑与调用链路
`同步记录` -> `BusinessTaskMaterializer` -> `BusinessTaskEntity`（初始状态） -> `任务仓储入口（后续 PR 接入）`。

### 复用文件
- `EverydayChain.Hub.Application/Services/SyncExecutionService.cs`
- `EverydayChain.Hub.Infrastructure/Repositories/SqlServerSyncUpsertRepository.cs`

### 参考文件
- `EverydayChain.Hub.Domain/Options/SyncTableOptions.cs`（字段建模风格）
- `EverydayChain.Hub.Tests/Repositories/SyncTaskConfigRepositoryTests.cs`（测试组织风格）

### 执行步骤
1. 新增 BusinessTask 领域实体与状态枚举。
2. 新增物化服务接口与实现，仅做映射与默认赋值。
3. 注册 DI。
4. 增加物化与实体校验测试。

### 预计改动文件
- 新增 8，修改 4，合计约 12。

### 交付结果
- 形成统一业务任务主实体，后续 API 与规则均围绕该实体推进。

### 验收标准
- 状态枚举覆盖 Created/Scanned/Dropped/FeedbackPending。
- 物化服务不包含扫描、格口、落格业务。
- 单测覆盖必填字段与默认状态。

### 评审人关注点
- 领域模型是否侵入同步实现细节。
- 状态定义是否可支撑后续链路。

---

## PR-02：语义与接口基线文档

### 建议标题
`docs(baseline): 固化状态语义、条码规则与API契约基线`

### 业务逻辑与调用链路
`业务术语定义` -> `状态迁移规则` -> `API 输入输出契约` -> `后续 PR 实现约束`。

### 复用文件
- `EverydayChain.Hub_详细业务背景开发指令_v2.md`
- `Oracle到SQLServer同步架构设计.md`

### 参考文件
- `README.md`（文档组织方式）
- `Oracle到SQLServer同步实施计划.md`（计划拆分表达方式）

### 执行步骤
1. 固化状态语义文档。
2. 固化条码规则文档。
3. 固化三类 API 基线文档。
4. 在总计划中建立引用关系。

### 预计改动文件
- 新增 5，修改 2，合计约 7。

### 交付结果
- 后续 PR 有统一术语和验收标准，降低语义漂移。

### 验收标准
- 三个 API 均定义幂等语义与失败语义。
- 同步回写与业务回传边界清晰。

### 评审人关注点
- 文档术语是否前后一致。
- 是否可直接映射到代码实现。

---

## PR-03：Host API 承载骨架

### 建议标题
`feat(host): 新增扫描/格口/落格三类API控制器骨架`

### 业务逻辑与调用链路
`外部 HTTP 请求` -> `Controller 参数校验` -> `Application 服务接口` -> `返回标准响应`。

### 复用文件
- `EverydayChain.Hub.Host/Program.cs`
- `EverydayChain.Hub.Host/Workers/SyncBackgroundWorker.cs`
- `EverydayChain.Hub.Host/Workers/RetentionBackgroundWorker.cs`

### 参考文件
- `EverydayChain.Hub.Host/appsettings.json`（配置说明风格）
- `EverydayChain.Hub.Application/Abstractions/Services/ISyncExecutionService.cs`（接口分层风格）

### 执行步骤
1. 启用 Controllers 与 Swagger。
2. 新增三类 Controller 与请求响应契约。
3. 保持 Worker 注册不变。
4. 增加 Controller 基础行为测试。

### 预计改动文件
- 新增 12，修改 4，合计约 16。

### 交付结果
- API 承载能力可用，后续业务服务可逐步接入。

### 验收标准
- Worker 与 API 可共存运行。
- Controller 不承载业务规则。
- Swagger 中文注释可见。

### 评审人关注点
- Host 层是否越层写业务逻辑。
- Program 启动项是否破坏现有 Worker。

---

## PR-04：条码解析与扫描输入

### 建议标题
`feat(application): 新增条码解析服务与扫描输入模型`

### 业务逻辑与调用链路
`扫描请求` -> `IBarcodeParser` -> `BarcodeParseResult` -> `ScanEventArgs` -> `后续匹配服务`。

### 复用文件
- `EverydayChain.Hub.Host/Controllers/ScanController.cs`（PR-03 新增）
- `EverydayChain.Hub.Application/Abstractions/Sync/IOracleStatusDrivenSourceReader.cs`

### 参考文件
- `EverydayChain.Hub.Infrastructure/Repositories/SyncTaskConfigRepository.cs`（参数校验与规范化风格）

### 执行步骤
1. 定义条码类型与解析结果。
2. 实现解析服务。
3. 定义扫描输入模型。
4. 接入 DI 并补测试。

### 预计改动文件
- 新增 8，修改 3，合计约 11。

### 交付结果
- 扫描数据在进入业务匹配前完成标准化。

### 验收标准
- 可区分拆零/整件/无效码。
- 对无效码返回统一错误语义。

### 评审人关注点
- 条码解析规则是否可扩展。
- 模型是否包含后续链路所需字段。

---

## PR-05：扫描匹配与任务执行

### 建议标题
`feat(application): 打通扫描匹配与任务执行状态推进`

### 业务逻辑与调用链路
`ScanController` -> `IScanMatchService` -> `ITaskExecutionService` -> `IBusinessTaskRepository` -> `状态更新`。

### 复用文件
- `EverydayChain.Hub.Application/Services/SyncExecutionService.cs`
- `EverydayChain.Hub.Infrastructure/Persistence/HubDbContext.cs`
- `EverydayChain.Hub.Infrastructure/Repositories/SqlServerSyncUpsertRepository.cs`

### 参考文件
- `EverydayChain.Hub.Infrastructure/Repositories/InMemorySyncBatchRepository.cs`（仓储实现风格）
- `EverydayChain.Hub.Tests/Repositories/InMemorySqlServerSyncUpsertRepository.cs`（测试替身风格）

### 执行步骤
1. 新增扫描匹配服务与执行服务。
2. 新增业务任务仓储抽象与实现。
3. 扫描接口接入执行链路。
4. 补 EF 映射与迁移、测试。

### 预计改动文件
- 新增 10，修改 6，合计约 16。

### 交付结果
- 扫描上传触发业务任务状态推进。

### 验收标准
- 匹配成功/失败均有明确结果。
- 扫描次数、状态、时间正确落库。

### 评审人关注点
- Application 抽象是否放在 `Application/Abstractions`。
- 迁移是否仅覆盖新增业务表/字段。

---

## PR-06：请求格口服务

### 建议标题
`feat(application): 新增请求格口解析服务`

### 业务逻辑与调用链路
`ChuteController` -> `IChuteResolveService` -> `业务任务读取` -> `格口规则计算` -> `返回格口结果`。

### 复用文件
- `EverydayChain.Hub.Host/Controllers/ChuteController.cs`
- `EverydayChain.Hub.Application/TaskExecution/Services/TaskExecutionService.cs`

### 参考文件
- `EverydayChain.Hub.Infrastructure/Repositories/SyncTaskConfigRepository.cs`（配置驱动解析思路）

### 执行步骤
1. 新增格口解析接口与实现。
2. 将 Chute API 接入解析服务。
3. 增加无任务、异常任务分支测试。

### 预计改动文件
- 新增 4，修改 4，合计约 8。

### 交付结果
- 请求格口 API 可返回目标格口与失败原因。

### 验收标准
- 返回结构包含任务标识与格口编码。
- 不在本 PR 写入落格状态。

### 评审人关注点
- 格口请求是否保持“只查询不确认落格”。
- 响应契约是否稳定可前后兼容。

---

## PR-07：落格回传服务

### 建议标题
`feat(application): 新增落格回传服务与状态闭环`

### 业务逻辑与调用链路
`DropFeedbackController` -> `IDropFeedbackService` -> `任务定位(TaskId/Barcode)` -> `Dropped/Exception 状态写入`。

### 复用文件
- `EverydayChain.Hub.Host/Controllers/DropFeedbackController.cs`
- `EverydayChain.Hub.Infrastructure/Repositories/BusinessTaskRepository.cs`（PR-05 新增）

### 参考文件
- `EverydayChain.Hub.Infrastructure/Sync/Writers/OracleRemoteStatusWriter.cs`（回写审计字段处理风格）

### 执行步骤
1. 新增落格回传服务接口与实现。
2. 接入 Controller 并支持双定位路径。
3. 补充成功/失败回传测试。

### 预计改动文件
- 新增 4，修改 5，合计约 9。

### 交付结果
- 实际落格结果可反映到本地任务生命周期。

### 验收标准
- 成功进入 Dropped，失败进入 Exception。
- 回传失败原因与时间可追踪。

### 评审人关注点
- 状态机是否出现非法跳转。
- 双定位策略是否存在歧义。

---

## PR-08：业务回传服务

### 建议标题
`feat(infrastructure): 新增业务回传写入器与回填流程`

### 业务逻辑与调用链路
`WmsFeedbackService` -> `查询待回传任务` -> `OracleWmsFeedbackWriter` -> `回传结果回填`。

### 复用文件
- `EverydayChain.Hub.Application/Abstractions/Sync/IOracleRemoteStatusWriter.cs`（边界参照）
- `EverydayChain.Hub.Infrastructure/Sync/Writers/OracleRemoteStatusWriter.cs`（Oracle 写入风格参照）

### 参考文件
- `EverydayChain.Hub.Infrastructure/Sync/Services/RemoteStatusConsumeService.cs`（批处理与写回节奏参照）

### 执行步骤
1. 新增业务回传服务与 Oracle 写入器。
2. 增加待回传任务筛选与幂等控制。
3. 回填回传状态与回传时间。
4. 补测试。

### 预计改动文件
- 新增 4，修改 6，合计约 10。

### 交付结果
- 形成落格后业务回传闭环，不影响同步自动回写。

### 验收标准
- 仅处理待回传任务。
- 回传成功/失败状态可区分、可重试。

### 评审人关注点
- 是否误复用同步自动回写通道。
- Oracle 写入是否有审计字段与幂等策略。

---

## PR-09：扫描/落格日志落库

### 建议标题
`feat(domain+infra): 新增扫描日志与落格日志聚合`

### 业务逻辑与调用链路
`扫描执行/落格回传` -> `日志聚合实体` -> `EF 配置` -> `日志表落库`。

### 复用文件
- `EverydayChain.Hub.Infrastructure/Persistence/HubDbContext.cs`
- `EverydayChain.Hub.Application/TaskExecution/Services/TaskExecutionService.cs`
- `EverydayChain.Hub.Application/DropFeedback/Services/DropFeedbackService.cs`

### 参考文件
- 现有 `Infrastructure/Persistence/EntityConfigurations/*`（实体配置风格）
- 现有 `Infrastructure/Migrations/*`（迁移命名风格）

### 执行步骤
1. 新增扫描日志与落格日志实体。
2. 新增 EF 配置并接入 DbContext。
3. 在关键服务写入日志。
4. 增加迁移与持久化测试。

### 预计改动文件
- 新增 6，修改 5，合计约 11。

### 交付结果
- 扫描与落格全链路具备审计轨迹。

### 验收标准
- 成功与失败都落日志。
- 日志包含任务标识、结果、原因、时间。

### 评审人关注点
- 热路径写日志是否造成性能风险。
- 日志字段是否满足排障最小闭环。

---

## PR-10：异常规则链路（波次/多标签/回流）

### 建议标题
`feat(application): 新增异常规则服务并接入任务执行链路`

### 业务逻辑与调用链路
`任务执行` -> `波次清理规则` -> `多标签决策` -> `回流决策` -> `执行结果`。

### 复用文件
- `EverydayChain.Hub.Application/TaskExecution/Services/TaskExecutionService.cs`
- `EverydayChain.Hub.Host/appsettings.json`（规则配置承载）

### 参考文件
- `EverydayChain.Hub.Infrastructure/Repositories/SyncTaskConfigRepository.cs`（配置解析与校验模式）

### 执行步骤
1. 新增三类规则抽象与实现。
2. 在任务执行中挂接规则判定。
3. 支持 dry-run 与审计输出。
4. 增加规则测试。

### 预计改动文件
- 新增 8，修改 5，合计约 13。

### 交付结果
- 异常业务分支可配置、可测试、可追踪。

### 验收标准
- 三类规则均可独立验证输入输出。
- dry-run 不执行破坏性操作。

### 评审人关注点
- 规则优先级是否有冲突。
- 配置缺省值与异常分支是否可预期。

---

## PR-11：补偿重试链路

### 建议标题
`feat(application): 新增业务回传补偿服务`

### 业务逻辑与调用链路
`补偿入口` -> `识别回传失败任务` -> `重试回传` -> `更新补偿状态与日志`。

### 复用文件
- `EverydayChain.Hub.Application/Feedback/Services/WmsFeedbackService.cs`
- `EverydayChain.Hub.Infrastructure/Repositories/BusinessTaskRepository.cs`
- `EverydayChain.Hub.Host/Program.cs`

### 参考文件
- `EverydayChain.Hub.Host/Workers/RetentionBackgroundWorker.cs`（后台任务组织方式）
- `EverydayChain.Hub.Infrastructure/Sync/Services/RemoteStatusConsumeService.cs`（批次处理风格）

### 执行步骤
1. 新增补偿服务接口与实现。
2. 支持按任务、按批次重试。
3. 记录补偿结果并回填任务状态。
4. 增加补偿测试。

### 预计改动文件
- 新增 3，修改 5，合计约 8。

### 交付结果
- 回传失败任务具备可追踪重试闭环。

### 验收标准
- 补偿可重入，重复触发不产生脏写。
- 成功关闭任务，失败保留待补偿标记。

### 评审人关注点
- 补偿重试是否有上限与节流。
- 失败日志是否足以支持人工介入。

---

## PR-12：联调收口与验收归档

### 建议标题
`chore(release): 全链路联调收口与验收归档`

### 业务逻辑与调用链路
`扫描上传` -> `请求格口` -> `落格回传` -> `业务回传` -> `补偿重试`（失败路径）-> `最终状态一致性校验`。

### 复用文件
- `EverydayChain.Hub.Host/Program.cs`
- `EverydayChain.Hub.Host/appsettings.json`
- `EverydayChain.Hub.Tests/*`

### 参考文件
- `README.md`（收口说明格式）
- `逐文件代码检查台账.md`（验收记录表达方式）

### 执行步骤
1. 执行端到端联调与回归。
2. 对齐文档、配置、代码术语。
3. 补充缺口测试并归档验收项。
4. 输出“已实现/未实现/后续计划”。

### 预计改动文件
- 新增 0~2，修改 6~10，合计约 8~12。

### 交付结果
- 形成可发布、可验收、可交接的最终版本。

### 验收标准
- 主链路与失败补偿链路均跑通。
- 构建测试全绿。
- 文档与代码一致。

### 评审人关注点
- 是否仍存在跨层实现泄漏。
- 是否存在遗漏的待确认项。

---

## 4. PR 标题/分支/评审关注点汇总清单（可直接排期）

| PR | 建议标题 | 建议分支名 | 评审关注点 |
|---|---|---|---|
| PR-01 | `feat(domain): 新增业务任务主模型与物化服务` | `feature/pr01-business-task-model` | 领域边界、状态建模完整性 |
| PR-02 | `docs(baseline): 固化状态语义、条码规则与API契约基线` | `feature/pr02-semantic-baseline` | 术语一致性、实现可映射性 |
| PR-03 | `feat(host): 新增扫描/格口/落格三类API控制器骨架` | `feature/pr03-host-api-bootstrap` | Host 是否越层、Worker 共存 |
| PR-04 | `feat(application): 新增条码解析服务与扫描输入模型` | `feature/pr04-barcode-and-scan-model` | 解析扩展性、输入模型完备性 |
| PR-05 | `feat(application): 打通扫描匹配与任务执行状态推进` | `feature/pr05-scan-match-and-execution` | 抽象归属、迁移准确性 |
| PR-06 | `feat(application): 新增请求格口解析服务` | `feature/pr06-chute-resolve` | 查询职责纯度、响应契约稳定 |
| PR-07 | `feat(application): 新增落格回传服务与状态闭环` | `feature/pr07-drop-feedback` | 状态机合法性、定位策略 |
| PR-08 | `feat(infrastructure): 新增业务回传写入器与回填流程` | `feature/pr08-wms-feedback` | 同步回写与业务回传隔离 |
| PR-09 | `feat(domain+infra): 新增扫描日志与落格日志聚合` | `feature/pr09-scan-drop-logs` | 热路径性能、审计字段完整 |
| PR-10 | `feat(application): 新增异常规则服务并接入任务执行链路` | `feature/pr10-exception-rules` | 规则优先级、配置边界 |
| PR-11 | `feat(application): 新增业务回传补偿服务` | `feature/pr11-compensation` | 重试幂等、失败可观测性 |
| PR-12 | `chore(release): 全链路联调收口与验收归档` | `feature/pr12-stabilization` | 端到端一致性、遗留风险清零 |

---

## 5. 每个 PR 描述模板（统一）

```md
## 本 PR 范围
- 目标：
- 不在范围：

## 业务链路
- 输入：
- 处理：
- 输出：

## 复用文件
1. 
2. 

## 参考文件
1. 
2. 

## 改动文件统计
- 新增：X
- 修改：Y
- 删除：Z
- 合计：X+Y+Z

## 交付结果
1. 
2. 

## 验收标准
- [ ] build 通过
- [ ] test 通过
- [ ] 本 PR 验收点 1
- [ ] 本 PR 验收点 2

## 评审关注点
1. 
2. 

## 待确认项
1. 
2. 
```
