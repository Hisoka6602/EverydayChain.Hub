# EverydayChain.Hub 分阶段执行 PR 规划与验收门禁（严格版）

## 1. 文档目的

本文档用于把 `EverydayChain.Hub` 的修正开发工作拆分为**可执行、可审查、可逐个合并**的 PR（Pull Request）清单，并为每个阶段配置**明确的验收门禁**。

本文档目标：

1. 防止 Copilot 一次性改动过大
2. 防止业务需求和技术实现再次跑偏
3. 防止新旧实现双轨并存且不删除
4. 防止“文档完成了，代码没有完成”
5. 保证每个阶段都能独立构建、独立测试、独立验收

---

## 2. 全局固定约束（所有 PR 都必须遵守）

### 2.1 不开发 UI

本项目不开发任何 UI。  
即使是：

- 总看板
- 码头看板
- 报表

也仅提供：

1. 查询 API
2. 导出 API

---

### 2.2 现有分拣机在线 API 必须保留

以下端点必须保留，禁止破坏兼容性：

1. 扫描上传接口
2. 请求格口接口
3. 落格回传接口

禁止出现：

1. 修改原路由导致对接方失效
2. 变更原请求字段语义
3. 变更原响应字段语义
4. 删除原控制器
5. 用新接口替代原在线接口

---

### 2.3 自动迁移必须保留

以下能力必须保留：

1. 自动迁移
2. 启动迁移检查
3. 新增表自动纳管
4. 分表模板迁移

---

### 2.4 自动分表必须保留

以下能力必须保留：

1. 自动分表
2. 分表预建
3. 分表读写
4. 分表聚合查询兼容

---

### 2.5 所有 PR 都必须说明旧实现删除清单

只要新增代码覆盖了原实现，就必须在 PR 说明里明确写出：

1. 哪些旧服务应删除
2. 哪些旧 DTO 应删除
3. 哪些旧查询应删除
4. 哪些旧配置应删除
5. 哪些旧测试应删除或替换

禁止“新代码加上了，旧代码不处理”。

---

## 3. 分阶段总览

本轮建议拆成以下 8 个执行阶段、10 个 PR：

### 阶段 1：业务模型收口
- PR-01：拆零/整件业务字段模型补齐与迁移

### 阶段 2：扫描闭环
- PR-02：扫描字段闭环改造
- PR-03：扫描链路兼容性测试与日志补强

### 阶段 3：WMS 回写闭环
- PR-04：真实业务字段回写模型与配置收口
- PR-05：WMS 回写执行链路与补偿收口

### 阶段 4：波次清理 API
- PR-06：波次清理正式 API 化

### 阶段 5：总看板 API
- PR-07：总看板查询 API

### 阶段 6：码头看板 API
- PR-08：码头看板查询 API

### 阶段 7：报表 API
- PR-09：报表查询与导出 API

### 阶段 8：业务查询与收口
- PR-10：业务任务/异常件/回流查询 API + 冗余实现删除

---

## 3.1 当前实现盘点（2026-04-17，PR-07 补全后基线）

> 盘点范围：`Domain`/`Application`/`Infrastructure`/`Host` 与 `EverydayChain.Hub.Tests` 当前主干代码。

| PR | 当前状态 | 盘点结论 |
|---|---|---|
| PR-01 业务模型收口 | ✅ 本轮补全完成 | 本轮已新增 `BusinessTaskSourceType`、`WaveRemark`、尺寸体积重量、`ScanCount`、`IsFeedbackReported`、`FeedbackTimeLocal`、`IsException` 等统一字段，并补齐 EF 配置与迁移 `20260417043253_AddBusinessTaskClosureFields`。 |
| PR-02 扫描字段闭环 | ✅ 本轮补全完成 | 扫描接口与多条码策略保持兼容，`TaskExecutionService` 已在扫描成功链路写入扫描时间、尺寸体积重量并递增 `ScanCount`，扫描日志链路保持不变。 |
| PR-03 扫描兼容测试与日志补强 | ✅ 已完成 | 扫描控制器与扫描链路测试已覆盖，日志落地链路存在并稳定。 |
| PR-04 回写模型与配置收口 | ✅ 本轮补全完成 | `WmsFeedbackOptions` 已补齐拆零/整件分流目标配置（Schema/Table/BusinessKey）与真实业务字段映射配置（扫描时间、尺寸体积重量、扫描次数、业务状态）。 |
| PR-05 回写执行与补偿收口 | ✅ 本轮补全完成 | `OracleWmsFeedbackGateway` 已按来源类型分流回写并覆盖真实业务字段；失败仍走既有补偿链路，`WmsFeedbackService`/`FeedbackCompensationService` 维持成功/失败状态闭环。 |
| PR-06 波次清理 API 化 | ✅ 本轮补全完成 | 本轮新增 `WaveCleanupController`、`WaveCleanupRequest/Response`、`dry-run` 与正式执行端点，并补齐控制器测试。 |
| PR-07 总看板 API | ✅ 本轮补全完成 | 本轮新增 `GlobalDashboardController` 与 `GlobalDashboardQueryService`，支持时间区间查询、波次维度聚合、整件/拆零统计、识别率、回流数、异常数、总体积与总重量。 |
| PR-08 码头看板 API | ❌ 未开始 | 代码中尚未提供码头看板查询 API。 |
| PR-09 报表查询与导出 API | ❌ 未开始 | 代码中尚未提供报表查询与导出 API。 |
| PR-10 业务查询与收口删除 | ❌ 未开始 | 代码中尚未提供业务任务/异常件/回流查询 API 与对应收口删除。 |

## 3.2 本轮补全结果

1. 新增 `Host/Controllers/WaveCleanupController.cs`，提供：
   - `POST /api/v1/wave-cleanup/dry-run`
   - `POST /api/v1/wave-cleanup/execute`
2. 新增 Host 契约：
   - `Host/Contracts/Requests/WaveCleanupRequest.cs`
   - `Host/Contracts/Responses/WaveCleanupResponse.cs`
3. 扩展应用层契约与实现：
   - `IWaveCleanupService` 新增 `DryRunByWaveCodeAsync`、`ExecuteByWaveCodeAsync`
   - `WaveCleanupService` 新增端点级 dry-run/正式执行分流能力
4. 新增测试：
   - `Tests/Host/Controllers/WaveCleanupControllerTests.cs`
   - `Tests/Host/Controllers/StubWaveCleanupService.cs`
5. 补齐统一业务任务字段（PR-01/PR-02）：
   - `Domain/Enums/BusinessTaskSourceType.cs`
   - `Domain/Aggregates/BusinessTaskAggregate/BusinessTaskEntity.cs`
   - `Infrastructure/Persistence/EntityConfigurations/BusinessTaskEntityTypeConfiguration.cs`
   - `Infrastructure/Migrations/20260417043253_AddBusinessTaskClosureFields.cs`
6. 扫描与回写链路字段闭环补齐：
   - `Application/TaskExecution/Services/TaskExecutionService.cs`
   - `Application/Feedback/Services/WmsFeedbackService.cs`
   - `Application/Feedback/Services/FeedbackCompensationService.cs`
   - `Application/Services/DropFeedbackService.cs`
   - `Application/Recirculation/Services/RecirculationService.cs`
7. 测试补齐：
    - `Tests/Services/TaskExecutionServiceTests.cs`
    - `Tests/Services/BusinessTaskMaterializerTests.cs`
8. 回写链路补齐（PR-04/PR-05）：
   - `Domain/Options/WmsFeedbackOptions.cs`
   - `Infrastructure/Integrations/OracleWmsFeedbackGateway.cs`
   - `Host/appsettings.json`
9. 总看板 API 补齐（PR-07）：
   - `Host/Controllers/GlobalDashboardController.cs`
   - `Host/Contracts/Requests/GlobalDashboardQueryRequest.cs`
   - `Host/Contracts/Responses/GlobalDashboardResponse.cs`
   - `Host/Contracts/Responses/WaveDashboardSummaryResponse.cs`
   - `Application/Abstractions/Queries/IGlobalDashboardQueryService.cs`
   - `Application/Queries/GlobalDashboardQueryService.cs`
   - `Application/Models/GlobalDashboardQueryRequest.cs`
   - `Application/Models/GlobalDashboardQueryResult.cs`
   - `Application/Models/WaveDashboardSummary.cs`
   - `Application/Abstractions/Persistence/IBusinessTaskRepository.cs`
   - `Infrastructure/Repositories/BusinessTaskRepository.cs`
10. 总看板测试补齐：
   - `Tests/Host/Controllers/GlobalDashboardControllerTests.cs`
   - `Tests/Host/Controllers/StubGlobalDashboardQueryService.cs`
   - `Tests/Services/GlobalDashboardQueryServiceTests.cs`

## 3.3 下一步优先补全建议（按顺序）

1. 推进 PR-08~PR-10 的查询类 API 与收口删除。
2. 补充 PR-04/PR-05 的联调验收记录（拆零/整件目标表字段抽样核对、失败补偿重试演练）。
3. 补充 PR-01/PR-02 的分表回归与迁移执行验收记录（含上线前 SQL 校验清单）。

---

# 4. 阶段 1：业务模型收口

## PR-01：拆零/整件业务字段模型补齐与迁移

### 4.1 目标

补齐后续扫描闭环、WMS 回写闭环、看板/报表查询所必需的业务字段模型，且不破坏现有在线接口。

### 4.2 本 PR 必须完成的内容

1. 明确本地业务承载方案  
   推荐方案：

   - 拆零本地业务实体
   - 整件本地业务实体
   - 统一业务任务实体（保留现有 `BusinessTaskEntity` 主链路）

2. 补齐至少以下字段承载：

   - 来源类型（拆零/整件）
   - 波次号
   - 波次备注（若源数据可取）
   - 条码
   - 目标格口
   - 实际格口
   - 最后扫描时间
   - 长
   - 宽
   - 高
   - 体积
   - 重量
   - 扫描次数
   - 回传标记
   - 回传时间
   - 异常状态
   - 回流状态

3. 补齐 EF Core 映射
4. 补齐迁移
5. 确保自动迁移仍然有效
6. 确保自动分表仍然有效
7. 如需要，补齐查询索引基础设计

### 4.3 本 PR 禁止事项

1. 禁止修改现有分拣机 API
2. 禁止提前新增看板 API
3. 禁止提前新增报表 API
4. 禁止只改文档不改模型
5. 禁止删除 `BusinessTaskEntity` 而没有兼容迁移方案

### 4.4 PR 说明必须写明

1. 新增了哪些实体
2. 修改了哪些实体
3. 新增了哪些迁移
4. 自动迁移是否受影响
5. 自动分表是否受影响
6. 旧模型里哪些字段/实现已废弃

### 4.5 验收门禁

#### A. 结构门禁
1. Domain 层已有补齐后的业务模型
2. Infrastructure 层已有 EF 配置
3. 迁移可成功生成/应用
4. Host 层现有控制器不受影响

#### B. 功能门禁
1. 新字段能承载扫描闭环
2. 新字段能承载回写闭环
3. 新字段能支撑后续统计查询
4. 来源类型能区分拆零/整件

#### C. 兼容门禁
1. 现有扫描接口不变
2. 现有格口接口不变
3. 现有落格接口不变
4. 自动迁移不失效
5. 自动分表不失效

#### D. 代码门禁
1. 不允许新旧模型职责完全重复
2. 若存在覆盖关系，PR 中必须明确待删除项
3. 不能引入新的通用平台抽象

---

# 5. 阶段 2：扫描闭环

## PR-02：扫描字段闭环改造

### 5.1 目标

保留现有扫描上传 API 的前提下，完成真实业务字段更新，而不是只更新统一任务状态。

### 5.2 本 PR 必须完成的内容

1. 保留 `ScanController` 路由与协议不变
2. 在扫描成功链路中补齐以下业务更新：

   - 最后扫描时间
   - 长
   - 宽
   - 高
   - 体积
   - 重量
   - 扫描次数递增

3. 明确多条码场景写入策略
4. 保留并继续写扫描日志
5. 保证状态推进和字段更新是一条闭环链路

### 5.3 本 PR 禁止事项

1. 禁止修改扫描接口路由
2. 禁止修改分拣机请求格式
3. 禁止只更新日志，不更新业务字段
4. 禁止只更新 `BusinessTaskEntity.Status`

### 5.4 PR 说明必须写明

1. 扫描后哪些字段会更新
2. 多条码场景如何处理
3. 哪些服务被替代
4. 哪些旧扫描逻辑应该删除

### 5.5 验收门禁

#### A. 接口门禁
1. 扫描端点地址不变
2. 请求结构兼容
3. 响应结构兼容

#### B. 功能门禁
1. 扫描后 `CLOSETIME` 语义字段已更新
2. 长宽高体积重量已更新
3. `scancount` 已递增
4. 扫描日志仍正常写入
5. 状态推进仍正常

#### C. 数据门禁
1. 拆零数据可更新
2. 整件数据可更新
3. 多条码策略明确且可重复验证

#### D. 回归门禁
1. 原有扫描测试通过
2. 新增扫描字段测试通过
3. 不影响自动迁移与自动分表

---

## PR-03：扫描链路兼容性测试与日志补强

### 5.6 目标

为 PR-02 补齐测试与兼容性验证，避免后续继续修改时破坏分拣机在线链路。

### 5.7 本 PR 必须完成的内容

1. 增加扫描字段闭环单元测试/集成测试
2. 增加多条码场景测试
3. 增加扫描日志落地验证
4. 增加字段更新失败隔离验证
5. 增加兼容性回归说明

### 5.8 本 PR 禁止事项

1. 禁止在本 PR 再追加新业务需求
2. 禁止混入看板/报表开发
3. 禁止修改 PR-02 已稳定的接口协议

### 5.9 验收门禁

#### A. 测试门禁
1. 扫描成功测试通过
2. 扫描失败测试通过
3. 多条码测试通过
4. 字段更新测试通过
5. 日志写入测试通过

#### B. 兼容门禁
1. 现有在线接口兼容
2. 无新增破坏性字段变更
3. 无路由变更

---

# 6. 阶段 3：WMS 回写闭环

## PR-04：真实业务字段回写模型与配置收口

### 6.1 目标

从“通用反馈表占位方案”收口到“真实业务字段回写方案”。

### 6.2 本 PR 必须完成的内容

1. 明确拆零表回写字段清单
2. 明确整件表回写字段清单
3. 明确回写配置模型
4. 明确回写字段映射
5. 明确本地待回写筛选条件
6. 明确回写成功/失败的本地状态语义

### 6.3 本 PR 禁止事项

1. 禁止继续使用纯占位 `IDX_FEEDBACK_TABLE` 方案作为最终实现
2. 禁止只定义通用状态列而不定义真实业务字段
3. 禁止跳过配置建模直接硬编码 SQL

### 6.4 验收门禁

#### A. 模型门禁
1. 拆零回写字段清单明确
2. 整件回写字段清单明确
3. 配置模型可表达真实字段映射
4. 本地待回写状态定义明确

#### B. 兼容门禁
1. 保留现有补偿框架
2. 不影响现有在线分拣机接口
3. 不影响自动迁移与自动分表

---

## PR-05：WMS 回写执行链路与补偿收口

### 6.5 目标

完成真实业务字段回写、回写失败补偿和本地状态闭环。

### 6.6 本 PR 必须完成的内容

1. 按真实字段执行 Oracle 回写
2. 回写内容至少覆盖：

   - 回传标记
   - 回传时间
   - 最后扫描时间
   - 长
   - 宽
   - 高
   - 体积
   - 重量
   - 扫描次数
   - 落格后的业务状态

3. 回写成功后更新本地状态
4. 回写失败后进入补偿链路
5. 保留现有补偿后台任务与失败重试机制
6. 补齐回写测试

### 6.7 本 PR 禁止事项

1. 禁止只写本地状态，不写 Oracle 真实字段
2. 禁止绕过现有补偿框架另起一套补偿实现
3. 禁止把失败吞掉不记录

### 6.8 验收门禁

#### A. 功能门禁
1. Oracle 真实字段已回写
2. 回写成功后本地状态更新正确
3. 回写失败后补偿任务可接管
4. 回写日志与错误日志完整

#### B. 数据门禁
1. 拆零回写路径通过
2. 整件回写路径通过
3. 批量回写路径通过

#### C. 回归门禁
1. 不影响扫描/格口/落格在线链路
2. 不影响自动迁移/自动分表
3. 原有补偿测试不失效

---

# 7. 阶段 4：波次清理 API

## PR-06：波次清理正式 API 化

### 7.1 目标

把内部波次清理服务升级为正式可调用的 API。

### 7.2 本 PR 必须完成的内容

1. 新增 `WaveCleanupController`
2. 提供 dry-run 端点
3. 提供正式执行端点
4. 支持指定 `WaveCode`
5. 返回识别数、清理数、dry-run 标志、消息
6. 保留审计日志
7. 只清理非终态数据

### 7.3 本 PR 禁止事项

1. 禁止开发 UI
2. 禁止直接删物理表
3. 禁止清理所有波次
4. 禁止不做审计记录

### 7.4 验收门禁

#### A. 接口门禁
1. 已新增波次清理 API
2. 支持 dry-run
3. 支持正式执行
4. 支持按波次号调用

#### B. 功能门禁
1. 仅清理指定波次
2. 仅清理非终态
3. 有清理结果回显
4. 有审计日志

#### C. 兼容门禁
1. 不影响现有分拣机接口
2. 不影响自动迁移与自动分表

---

# 8. 阶段 5：总看板 API

## PR-07：总看板查询 API

### 8.1 目标

以 API 形式提供总看板统计能力，不开发 UI。

### 8.2 本 PR 必须完成的内容

新增总看板查询端点，支持：

1. 时间区间查询
2. 波次维度聚合
3. 总件数
4. 未分拣数量
5. 总体分拣进度
6. 整件总数
7. 整件未分拣
8. 整件分拣进度
9. 拆零总数
10. 拆零未分拣
11. 拆零分拣进度
12. 识别率
13. 回流数
14. 异常数
15. 测量总体积
16. 测量总重量

### 8.3 本 PR 禁止事项

1. 禁止开发页面
2. 禁止控制器直接拼复杂业务查询
3. 禁止统计口径散落在多个类中
4. 禁止跳过识别率/回流/异常统计

### 8.4 验收门禁

#### A. API 门禁
1. 已新增查询端点
2. 支持时间区间
3. 响应结构稳定

#### B. 统计门禁
1. 整件/拆零统计分开可验证
2. 总件数正确
3. 未分拣数正确
4. 识别率正确
5. 回流数正确
6. 异常数正确
7. 总体积/总重量正确

#### C. 性能门禁
1. 查询不会走明显 N+1
2. 分表场景可正常查询
3. 不影响自动迁移/自动分表

---

# 9. 阶段 6：码头看板 API

## PR-08：码头看板查询 API

### 9.1 目标

以 API 形式提供码头看板能力，不开发 UI。

### 9.2 本 PR 必须完成的内容

新增码头看板查询端点，支持：

1. 默认当天
2. 波次选项卡/按波次筛选
3. 拆零未分拣数
4. 整件未分拣数
5. 回流数
6. 异常数
7. 分拣进度
8. 已分拣总数

### 9.3 特殊规则

必须落实：

- 只有 7 号码头显示异常数

### 9.4 本 PR 禁止事项

1. 禁止开发 UI
2. 禁止忽略 7 号码头异常规则
3. 禁止和总看板统计口径不一致

### 9.5 验收门禁

#### A. API 门禁
1. 已新增码头看板查询 API
2. 支持默认当天
3. 支持按波次查询

#### B. 业务门禁
1. 拆零/整件未分拣统计正确
2. 回流数正确
3. 分拣进度正确
4. 已分拣总数正确
5. 仅 7 号码头显示异常数规则已落实

#### C. 兼容门禁
1. 不影响原在线接口
2. 不影响自动迁移/自动分表

---

# 10. 阶段 7：报表 API

## PR-09：报表查询与导出 API

### 10.1 目标

提供报表查询 API 和导出 API，不开发 UI。

### 10.2 本 PR 必须完成的内容

1. 报表查询 API
2. 报表导出 API
3. 支持时间范围
4. 支持码头维度
5. 返回：

   - 拆零总数
   - 整件总数
   - 拆零分拣数
   - 整件分拣数
   - 回流数
   - 异常数（7 号码头规则）

6. 导出格式至少支持：
   - CSV
   - Excel（二选一可先落地，推荐 CSV 先行）

### 10.3 本 PR 禁止事项

1. 禁止开发报表 UI
2. 禁止只做导出不做查询
3. 禁止统计口径和码头看板不一致

### 10.4 验收门禁

#### A. API 门禁
1. 已新增报表查询 API
2. 已新增导出 API
3. 支持时间范围与码头条件

#### B. 统计门禁
1. 拆零总数正确
2. 整件总数正确
3. 拆零分拣数正确
4. 整件分拣数正确
5. 回流数正确
6. 7 号码头异常数规则正确

#### C. 导出门禁
1. 导出文件格式正确
2. 导出内容与查询结果口径一致
3. 大结果量下不会直接阻塞主链路

---

# 11. 阶段 8：业务查询与收口

## PR-10：业务任务/异常件/回流查询 API + 冗余实现删除

### 11.1 目标

提供业务查询能力，并对前面阶段产生的重复/废弃实现做收口删除。

### 11.2 本 PR 必须完成的内容

新增至少以下查询 API：

1. 业务任务查询
2. 异常件查询
3. 回流记录查询

支持条件：

1. 时间范围
2. 波次号
3. 条码
4. 码头
5. 格口
6. 分页

同时必须：

1. 删除已被覆盖的旧 DTO
2. 删除已被覆盖的旧查询服务
3. 删除无效占位配置
4. 更新 README 和执行文档

### 11.3 本 PR 禁止事项

1. 禁止只新增查询不删除旧实现
2. 禁止把冗余清理拖到以后
3. 禁止不更新文档

### 11.4 验收门禁

#### A. API 门禁
1. 业务任务查询可用
2. 异常件查询可用
3. 回流查询可用
4. 支持分页
5. 支持时间/波次/条码等筛选

#### B. 收口门禁
1. 已明确删除旧 DTO
2. 已明确删除旧服务
3. 已明确删除旧配置
4. 新旧逻辑不再双轨并存

#### C. 文档门禁
1. README 已更新
2. 执行文档已更新
3. PR 说明已写明删除清单

---

# 12. 所有 PR 通用验收门禁

以下门禁适用于所有 PR。

## 12.1 构建门禁

每个 PR 合并前必须满足：

1. `dotnet build EverydayChain.Hub.sln` 通过
2. `dotnet test EverydayChain.Hub.sln --no-build` 通过
3. 无新增 Warning 或 Warning 已明确说明

---

## 12.2 架构门禁

1. Domain 只放领域模型/枚举/值对象
2. Application 只放应用服务/DTO/抽象
3. Infrastructure 放仓储实现/EF/Oracle 实现
4. Host 放控制器/请求响应/后台任务入口
5. 不允许分层边界再次混乱

---

## 12.3 兼容门禁

1. 原有分拣机在线接口必须保持兼容
2. 自动迁移必须正常
3. 自动分表必须正常
4. 原有同步链路不能被破坏

---

## 12.4 删除门禁

凡是覆盖旧实现的 PR，必须附带：

1. 删除清单
2. 替代关系
3. 风险说明
4. 回滚说明（如有）

---

## 12.5 防跑偏门禁

若一个 PR 出现以下任意情况，则判定为跑偏：

1. 开发 UI
2. 优先扩平台治理能力
3. 继续抽象新平台框架
4. 不直接服务业务闭环
5. 只改文档不改代码
6. 新旧实现并存但不删除旧实现

---

# 13. 给 Copilot 的最终执行要求（可直接复制）

从现在开始，`EverydayChain.Hub` 必须严格按“分阶段执行 PR 规划与验收门禁”推进。  
所有实现必须按 PR-01 到 PR-10 的顺序逐步落地，禁止跳阶段、禁止大杂烩式 PR、禁止一次性同时做多阶段业务。  
本项目不开发 UI，只提供 API；现有分拣机在线接口必须保留；自动迁移和自动分表必须保留。  
每个 PR 开始前，必须先说明：本次 PR 编号、目标、影响范围、保留项、替代项、删除清单、验收门禁。  
若无法明确说明本 PR 对应哪个阶段、补哪条业务闭环、删哪些旧实现，则不得开始编码。
