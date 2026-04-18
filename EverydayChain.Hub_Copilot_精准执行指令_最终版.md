# EverydayChain.Hub - Copilot 精准执行指令（最终版，超严格门禁）

> 目标：允许 Copilot 分多个 PR 完成，但**绝对不允许跑偏**。  
> 本文档基于当前项目已确认的真实业务语义编写，所有实现必须严格遵循本文档。  
> 本文档中的“必须 / 禁止 / 不允许”均为硬约束，不是建议。

---

# 0. 最终目标（必须先理解，再开始编码）

本次改造的唯一正确目标是：

- **彻底删除本地 SQL Server 中作为镜像表存在的 `IDX_PICKTOLIGHT_CARTON1`、`IDX_PICKTOWCS2`**
- **远端 Oracle 源表仍然保留**：
  - 拆零源表：`IDX_PICKTOLIGHT_CARTON1`
  - 整件源表：`IDX_PICKTOWCS2`
- **本地只保留 `business_tasks` 作为业务主表**
- **同步链路改为**：
  - 读取远端 Oracle 源表待处理记录
  - 直接投影到本地 `business_tasks`
  - 按配置决定是否回写远端状态列
- **业务结果回写链路改为**：
  - 拆零任务只回写 `IDX_PICKTOLIGHT_CARTON1`
  - 整件任务只回写 `IDX_PICKTOWCS2`
- **不需要考虑历史数据迁移**
  - 可以直接清空本地旧数据
  - 不需要兼容旧本地镜像表数据
- **不能影响现有业务能力**
- **不能退化现有查询性能**
- **不能破坏现有对外 API**
- **不能保留任何本地镜像表双轨实现**

---

# 1. 已确认的真实业务语义（必须严格执行）

## 1.1 本地唯一业务主表
本地唯一业务主表是：

- `business_tasks`

本地不应该再保留任何以下镜像表语义：

- `IDX_PICKTOLIGHT_CARTON1`
- `IDX_PICKTOWCS2`

## 1.2 只有两种来源
系统中只允许两种业务来源：

- 拆零 → `IDX_PICKTOLIGHT_CARTON1`
- 整件 → `IDX_PICKTOWCS2`

不允许第三种默认来源，不允许默认兜底表，不允许 `IDX_SPLIT_TASK`。

## 1.3 同步状态回写
同步成功后的远端状态回写，也只允许回写当前消费的那张远端源表：

- 拆零同步 → 回写 `IDX_PICKTOLIGHT_CARTON1`
- 整件同步 → 回写 `IDX_PICKTOWCS2`

同步状态回写必须继续支持配置化：

- `StatusColumnName`
- `PendingStatusValue`
- `CompletedStatusValue`
- `ShouldWriteBackRemoteStatus`

## 1.4 业务结果回写
扫描、落格、业务状态推进后的结果回写，也只允许：

- 拆零业务任务 → 回写 `IDX_PICKTOLIGHT_CARTON1`
- 整件业务任务 → 回写 `IDX_PICKTOWCS2`

## 1.5 目标格口来源
**目标格口号不是从远端 Oracle 表读取。**

目标格口号必须继续由**条码解析链路**决定，不允许由同步投影层赋值。

硬规则：

- 同步配置中禁止出现 `TargetChuteColumn`
- 同步投影层禁止给 `business_tasks.TargetChuteCode` 赋值
- `TargetChuteCode` 只能由条码解析业务链路决定

## 1.6 尺寸重量来源
**尺寸重量不从 Oracle 源表同步到 `business_tasks`。**

本地 `business_tasks` 中以下字段：

- `LengthMm`
- `WidthMm`
- `HeightMm`
- `VolumeMm3`
- `WeightGram`

只允许由**扫描链路 / DWS 结果**写入。

硬规则：

- 同步配置中禁止出现：
  - `LengthColumn`
  - `WidthColumn`
  - `HeightColumn`
  - `VolumeColumn`
  - `WeightColumn`
- `WmsFeedback` 中保留同名字段作为**出站回写映射**
- 禁止因为同步层不需要入站映射，就删掉业务回写层的出站映射

## 1.7 TaskCode 规则
当前项目中，`TaskCode` 不需要独立配置，不需要单独从另一列映射。

硬规则：

- 同步配置中禁止出现 `TaskCodeColumn`
- `TaskCode` 统一由 `BusinessKey` 派生
- 规则固定为：
  - 拆零：`TaskCode = CARTONNO`
  - 整件：`TaskCode = SKUID`

---

# 2. 最终配置目标（必须落实）

## 2.1 SyncJob.Tables 最终允许保留的配置项

每个同步表最终只允许保留以下语义配置：

- `TableCode`
- `Enabled`
- `SourceSchema`
- `SourceTable`
- `TargetLogicalTable`
- `CursorColumn`
- `StartTimeLocal`
- `PageSize`
- `PollingIntervalSeconds`
- `MaxLagMinutes`
- `Priority`
- `SyncMode`
- `StatusColumnName`
- `PendingStatusValue`
- `CompletedStatusValue`
- `ShouldWriteBackRemoteStatus`
- `StatusBatchSize`
- `WriteBackCompletedTimeColumnName`
- `WriteBackBatchIdColumnName`
- `SourceType`
- `BusinessKeyColumn`
- `BarcodeColumn`
- `WaveCodeColumn`
- `WaveRemarkColumn`
- `UniqueKeys`
- `ExcludedColumns`
- `Delete`
- `Retention`

## 2.2 SyncJob.Tables 最终必须删除的配置项

以下配置项在最终版本中**禁止出现**：

- `TargetChuteColumn`
- `TaskCodeColumn`
- `LengthColumn`
- `WidthColumn`
- `HeightColumn`
- `VolumeColumn`
- `WeightColumn`

## 2.3 WmsFeedback 最终允许保留的配置项

只允许保留：

- `Enabled`
- `SplitSchema`
- `SplitTable`
- `SplitBusinessKeyColumn`
- `FullCaseSchema`
- `FullCaseTable`
- `FullCaseBusinessKeyColumn`
- `FeedbackStatusColumn`
- `FeedbackCompletedValue`
- `FeedbackTimeColumn`
- `ActualChuteColumn`
- `ScanTimeColumn`
- `LengthColumn`
- `WidthColumn`
- `HeightColumn`
- `VolumeColumn`
- `WeightColumn`
- `ScanCountColumn`
- `BusinessStatusColumn`
- `CommandTimeoutSeconds`

## 2.4 WmsFeedback 最终必须删除的配置项

以下配置项在最终版本中**禁止出现**：

- `Schema`
- `Table`
- `BusinessKeyColumn`

以及任何默认目标表、默认回写表、`IDX_SPLIT_TASK` 相关语义。

---

# 3. 最终代码设计原则（必须遵守）

## 3.1 同步层职责边界
同步层只负责：

- 读取远端待处理数据
- 根据配置提取：
  - 业务键
  - 条码
  - 波次号
  - 波次备注
  - 来源类型
- 投影到 `business_tasks`
- 按配置决定是否回写远端状态

同步层禁止负责：

- 目标格口解析
- 尺寸重量赋值
- 落格逻辑
- 业务结果回写逻辑

## 3.2 扫描链路职责边界
扫描链路负责：

- 条码解析
- 目标格口解析
- 扫描时间写入
- 尺寸重量写入
- 扫描次数递增

## 3.3 业务回写链路职责边界
业务回写链路负责：

- 按 `SourceType` 路由远端目标表
- 把本地业务结果回写到：
  - `IDX_PICKTOLIGHT_CARTON1`
  - `IDX_PICKTOWCS2`

## 3.4 幂等规则
本地 `business_tasks` 的同步幂等键必须是：

- `SourceTableCode + BusinessKey`

必须增加唯一索引，防止重复投影。

## 3.5 运行态保护规则
源端重复同步时，不允许覆盖本地运行态字段。

### 允许源端覆盖的字段
- `WaveCode`
- `WaveRemark`
- `Barcode`（仅在未进入扫描运行态前可谨慎更新，最终实现建议只初始化不覆盖）
- `UpdatedTimeLocal`

### 禁止源端覆盖的字段
- `Status`
- `FeedbackStatus`
- `ScannedAtLocal`
- `DroppedAtLocal`
- `ActualChuteCode`
- `TargetChuteCode`
- `DeviceCode`
- `FailureReason`
- `IsException`
- `IsRecirculated`
- `ScanRetryCount`
- `IsFeedbackReported`
- `FeedbackTimeLocal`
- `LengthMm`
- `WidthMm`
- `HeightMm`
- `VolumeMm3`
- `WeightGram`
- `ScanCount`

---

# 4. 分 PR 实施计划（建议必须按顺序执行）

---

# PR-1：建立 business_tasks 直投影链路（只新增，不删旧实现）

## 4.1 PR-1 目标
先建立新的业务投影与幂等合并链路，但暂时不删除旧镜像实现，方便小步提交与验证。

## 4.2 PR-1 必须新增/修改的文件

### Application 层新增
- `EverydayChain.Hub.Application/Abstractions/Services/IBusinessTaskProjectionService.cs`
- `EverydayChain.Hub.Application/Models/BusinessTaskProjectionRow.cs`
- `EverydayChain.Hub.Application/Models/BusinessTaskProjectionRequest.cs`
- `EverydayChain.Hub.Application/Models/BusinessTaskProjectionResult.cs`
- `EverydayChain.Hub.Application/Services/BusinessTaskProjectionService.cs`

### Application 层新增同步抽象
- `EverydayChain.Hub.Application/Abstractions/Sync/IBusinessTaskStatusConsumeService.cs`

### Infrastructure 层新增
- `EverydayChain.Hub.Infrastructure/Sync/Services/BusinessTaskStatusConsumeService.cs`

### Application 层持久化抽象修改
- `EverydayChain.Hub.Application/Abstractions/Persistence/IBusinessTaskRepository.cs`

### Infrastructure 层持久化实现修改
- `EverydayChain.Hub.Infrastructure/Repositories/BusinessTaskRepository.cs`
- `EverydayChain.Hub.Infrastructure/Persistence/EntityConfigurations/BusinessTaskEntityTypeConfiguration.cs`

### 配置模型修改
- `EverydayChain.Hub.Domain/Options/SyncTableOptions.cs`
- `EverydayChain.Hub.Domain/Sync/SyncTableDefinition.cs`
- `EverydayChain.Hub.Infrastructure/Repositories/SyncTaskConfigRepository.cs`

## 4.3 PR-1 必须实现的能力

### 4.3.1 新增投影模型 BusinessTaskProjectionRow
必须包含至少以下字段：

- `SourceTableCode`
- `SourceType`
- `BusinessKey`
- `Barcode`
- `WaveCode`
- `WaveRemark`
- `ProjectedTimeLocal`

禁止包含以下字段：
- `TargetChuteCode`
- `LengthMm`
- `WidthMm`
- `HeightMm`
- `VolumeMm3`
- `WeightGram`

### 4.3.2 新增投影服务 BusinessTaskProjectionService
职责只包括：

- 校验输入
- 标准化文本
- 构造 `BusinessTaskEntity`
- 设置：
  - `TaskCode = BusinessKey`
  - `SourceTableCode`
  - `SourceType`
  - `BusinessKey`
  - `Barcode`
  - `WaveCode`
  - `WaveRemark`
- 调用 `RefreshQueryFields()`

禁止在这里直接访问数据库。

### 4.3.3 扩展 IBusinessTaskRepository
必须新增能力：

- 按 `SourceTableCode + BusinessKey` 查询
- 按投影规则执行幂等 Upsert

### 4.3.4 增加唯一索引
在 `BusinessTaskEntityTypeConfiguration` 中增加联合唯一索引：

- `SourceTableCode + BusinessKey`

并保留现有高频查询索引。

### 4.3.5 新增业务消费服务 BusinessTaskStatusConsumeService
职责必须是：

1. 调用 `IOracleStatusDrivenSourceReader`
2. 读取待处理行，必须保留 `__RowId`
3. 根据配置读取：
   - `BusinessKeyColumn`
   - `BarcodeColumn`
   - `WaveCodeColumn`
   - `WaveRemarkColumn`
4. 构造 `BusinessTaskProjectionRow`
5. 调用 `IBusinessTaskProjectionService`
6. 调用 `IBusinessTaskRepository` 执行幂等 Upsert
7. 若 `ShouldWriteBackRemoteStatus=true`，则调用 `IOracleRemoteStatusWriter` 按 `ROWID` 回写远端状态
8. 若 `ShouldWriteBackRemoteStatus=false`，则不回写远端状态
9. 若 `PendingStatusValue=null`，则必须生成 `IS NULL` 条件
10. `CompletedStatusValue` 必须来自配置，禁止写死

## 4.4 PR-1 测试门禁

必须新增测试：

- `BusinessTaskProjectionServiceTests`
- `BusinessTaskStatusConsumeServiceTests`
- `BusinessTaskRepositoryProjectionUpsertTests`

至少覆盖以下场景：

1. 拆零源表行成功投影到 `business_tasks`
2. 整件源表行成功投影到 `business_tasks`
3. 同业务键重复消费不产生重复数据
4. `TaskCode` 正确等于 `BusinessKey`
5. 已有任务再次投影时，不覆盖运行态字段
6. `PendingStatusValue = null` 时生成 `IS NULL`
7. `ShouldWriteBackRemoteStatus = true` 时发生远端状态回写
8. `ShouldWriteBackRemoteStatus = false` 时不回写
9. `CompletedStatusValue` 配置修改后，回写值同步变化
10. 缺失 `__RowId` 时不得崩溃，且不得误回写

## 4.5 PR-1 禁止事项

- 禁止修改对外 API 契约
- 禁止在 Repository 中硬编码投影逻辑
- 禁止在 Gateway 中处理业务投影
- 禁止把 `TargetChuteCode` 从 Oracle 源表映射到 `business_tasks`
- 禁止把尺寸重量从 Oracle 源表映射到 `business_tasks`
- 禁止新增 `TaskCodeColumn`
- 禁止新增 `TargetChuteColumn`
- 禁止新增 `LengthColumn/WidthColumn/HeightColumn/VolumeColumn/WeightColumn` 到同步配置

---

# PR-2：切换同步主路径到 business_tasks

## 5.1 PR-2 目标
将这两个同步任务的主路径切到：

- 读取远端 Oracle
- 直接投影到 `business_tasks`
- 按配置回写远端状态

不再允许写本地镜像表。

## 5.2 PR-2 必须修改的文件
- `EverydayChain.Hub.Application/Services/SyncExecutionService.cs`
- `EverydayChain.Hub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
- `EverydayChain.Hub.Host/appsettings.json`

## 5.3 PR-2 必须实现的功能

### 5.3.1 切换 SyncExecutionService 主路径
对于这两个 WMS 同步任务：

- `WmsSplitPickToLightCarton`
- `WmsPickToWcs`

在 `StatusDriven` 模式下必须调用：

- `IBusinessTaskStatusConsumeService`

禁止再调用旧镜像表追加路径。

### 5.3.2 调整配置
在 `appsettings.json` 中：

- `SourceSchema` 保留
- `SourceTable` 保留
- `StatusColumnName` 保留
- `PendingStatusValue` 保留
- `CompletedStatusValue` 保留
- `ShouldWriteBackRemoteStatus` 保留
- `CursorColumn` 保留
- `StartTimeLocal` 保留
- `PageSize` 保留
- `PollingIntervalSeconds` 保留
- `MaxLagMinutes` 保留

同时：

- `TargetLogicalTable` 必须统一改成 `business_tasks`

新增投影配置仅允许：

- `SourceType`
- `BusinessKeyColumn`
- `BarcodeColumn`
- `WaveCodeColumn`
- `WaveRemarkColumn`

## 5.4 PR-2 测试门禁

必须新增或修改测试，覆盖：

1. 拆零同步任务直接进入 `business_tasks`
2. 整件同步任务直接进入 `business_tasks`
3. 不再写本地镜像表
4. `StatusColumnName` / `PendingStatusValue` / `CompletedStatusValue` 仍然有效
5. `ShouldWriteBackRemoteStatus=false` 时只写本地不回写远端
6. `ShouldWriteBackRemoteStatus=true` 时回写远端状态
7. `CompletedStatusValue` 变更时，回写值同步变化

## 5.5 PR-2 禁止事项

- 禁止继续双写本地镜像表
- 禁止保留“异常时退回镜像表”的兼容分支
- 禁止把 `business_tasks` 当作原样追加表使用
- 禁止删除同步状态回写配置能力

---

# PR-3：彻底删除本地镜像表实现

## 6.1 PR-3 目标
彻底删除本地 `IDX_PICKTOLIGHT_CARTON1`、`IDX_PICKTOWCS2` 的所有代码、映射、配置、迁移残留。

## 6.2 PR-3 必须删除/修改的文件

### Domain 层删除
- `EverydayChain.Hub.Domain/Aggregates/WmsSplitPickToLightCartonAggregate/WmsSplitPickToLightCartonEntity.cs`
- `EverydayChain.Hub.Domain/Aggregates/WmsPickToWcsAggregate/WmsPickToWcsEntity.cs`

### Infrastructure 层修改
- `EverydayChain.Hub.Infrastructure/Persistence/HubDbContext.cs`

必须删除：
- `WmsSplitPickToLightCartons`
- `WmsPickToWcsTasks`
- `ConfigureWmsSplitPickToLightCartonEntity`
- `ConfigureWmsPickToWcsEntity`
- 相关逻辑表名常量
- 相关动态分表映射逻辑

### Sync 旧实现删除或断开
- `EverydayChain.Hub.Infrastructure/Sync/Writers/SqlServerAppendOnlyWriter.cs`
- `EverydayChain.Hub.Infrastructure/Sync/Services/RemoteStatusConsumeService.cs`

要求：
- 若这两个文件仅服务镜像表路径，直接删除
- 若仍有其他用途，可保留文件，但必须完全断开这两个 WMS 表的调用链

### 配置删除
- 删除所有本地镜像表相关配置语义
- 删除分表预建 / 保留期治理中对本地镜像表的依赖

## 6.3 PR-3 测试门禁

必须新增架构防回退测试：

1. `HubDbContext` 中不再存在本地 `IDX_*` 映射
2. 模型快照中不再存在本地 `IDX_*`
3. 同步配置中不再存在本地目标为 `IDX_*` 的语义
4. 查询主路径只依赖 `business_tasks`

## 6.4 PR-3 禁止事项

- 禁止只停用不删除
- 禁止把本地镜像表改名后继续保留
- 禁止留下 TODO / 临时兼容 / 以后再删
- 禁止保留未引用死代码

---

# PR-4：重构 WmsFeedback，去掉默认回写目标表，完成最终收口

## 7.1 PR-4 目标
重构业务回写配置和代码，彻底删除默认回写表语义，只保留：

- 拆零 → `IDX_PICKTOLIGHT_CARTON1`
- 整件 → `IDX_PICKTOWCS2`

## 7.2 PR-4 必须修改的文件
- `EverydayChain.Hub.Domain/Options/WmsFeedbackOptions.cs`
- `EverydayChain.Hub.Infrastructure/Integrations/OracleWmsFeedbackGateway.cs`
- `EverydayChain.Hub.Host/appsettings.json`

## 7.3 PR-4 必须实现的功能

### 7.3.1 WmsFeedbackOptions 收敛
必须删除以下字段：
- `Schema`
- `Table`
- `BusinessKeyColumn`

只保留：
- `SplitSchema`
- `SplitTable`
- `SplitBusinessKeyColumn`
- `FullCaseSchema`
- `FullCaseTable`
- `FullCaseBusinessKeyColumn`
- 业务结果回写字段配置

### 7.3.2 OracleWmsFeedbackGateway 改造
`ResolveTargetBySourceType` 必须改为：

- `SourceType == Split` → 返回拆零目标
- `SourceType == FullCase` → 返回整件目标
- 否则直接抛出中文异常：

建议异常信息：
- `不支持的业务来源类型，无法确定 WMS 回写目标表。`

禁止再保留默认回退表语义。

### 7.3.3 WmsFeedback 配置最终形态
最终只允许配置：
- 拆零目标表
- 整件目标表
- 业务回写列映射

禁止再出现：
- `IDX_SPLIT_TASK`
- 默认兜底表
- 默认业务键列语义

## 7.4 PR-4 测试门禁

必须新增或修改测试，覆盖：

1. 拆零任务只回写 `IDX_PICKTOLIGHT_CARTON1`
2. 整件任务只回写 `IDX_PICKTOWCS2`
3. `SourceType` 非法时直接抛中文异常
4. 不再存在默认回退表
5. 长宽高体积重量字段仍能正常从本地回写远端 Oracle
6. 扫描时间、扫描次数、业务状态仍能正常回写

## 7.5 PR-4 禁止事项

- 禁止保留 `IDX_SPLIT_TASK`
- 禁止保留 `Schema/Table/BusinessKeyColumn` 默认语义
- 禁止为了兼容历史逻辑继续回退默认目标表

---

# PR-5：迁移基线重建 + 性能收口 + 全量回归

## 8.1 PR-5 目标
在删除旧镜像表与默认回写表逻辑后，完成迁移基线重建、性能收口、全量测试回归。

## 8.2 必须执行的内容

### 8.2.1 重建 EF 迁移基线
删除旧迁移链，重建新的基线迁移。
新的模型中不得再包含：

- 本地 `IDX_PICKTOLIGHT_CARTON1`
- 本地 `IDX_PICKTOWCS2`

### 8.2.2 核查 business_tasks 索引
必须确保以下索引合理存在：

- `SourceTableCode + BusinessKey` 唯一索引
- `NormalizedBarcode`
- `NormalizedWaveCode`
- `ResolvedDockCode`
- `Status`
- `FeedbackStatus`
- `CreatedTimeLocal`
- `UpdatedTimeLocal`

### 8.2.3 查询服务回归
必须验证以下服务继续只依赖 `business_tasks`：

- `GlobalDashboardQueryService`
- `DockDashboardQueryService`
- `SortingReportQueryService`
- `BusinessTaskReadService`

### 8.2.4 业务能力回归
必须验证：

- 扫描上传
- 请求格口
- 落格回传
- 业务回写
- 补偿重试
- 看板
- 报表
- 异常件 / 回流查询

全部不退化。

## 8.3 PR-5 测试门禁

必须通过至少以下测试：

- `TaskExecutionServiceTests`
- `ChuteQueryServiceTests`
- `DropFeedbackServiceTests`
- `WmsFeedbackServiceTests`
- `FeedbackCompensationServiceTests`
- `GlobalDashboardQueryServiceTests`
- `DockDashboardQueryServiceTests`
- `SortingReportQueryServiceTests`
- `BusinessTaskReadServiceTests`

并新增：

- `BusinessTaskSingleSourceArchitectureTests`
- `BusinessTaskStatusWriteBackConfigurationTests`
- `BusinessTaskIndexCoverageTests`

## 8.4 PR-5 禁止事项

- 禁止退化查询到内存分页
- 禁止保留被覆盖旧实现
- 禁止为了通过测试绕开真实业务链路
- 禁止删掉关键索引

---

# 5. 最终配置文件目标（必须输出为最终状态）

## 9.1 SyncJob.Tables 最终必须类似如下语义

### 拆零
- `SourceSchema = WMS_USER_431`
- `SourceTable = IDX_PICKTOLIGHT_CARTON1`
- `TargetLogicalTable = business_tasks`
- `CursorColumn = ADDTIME`
- `StartTimeLocal = 2016-03-01 00:00:00`
- `PageSize = 500`
- `PollingIntervalSeconds = 30`
- `MaxLagMinutes = 10`
- `SyncMode = StatusDriven`
- `StatusColumnName = TASKPROCESS`
- `PendingStatusValue = N`
- `CompletedStatusValue = Y`
- `ShouldWriteBackRemoteStatus = true`
- `SourceType = Split`
- `BusinessKeyColumn = CARTONNO`
- `BarcodeColumn = CARTONNO`
- `WaveCodeColumn = WAVENO`
- `WaveRemarkColumn = DESCR`

### 整件
- `SourceSchema = WMS_USER_431`
- `SourceTable = IDX_PICKTOWCS2`
- `TargetLogicalTable = business_tasks`
- `CursorColumn = ADDTIME`
- `StartTimeLocal = 2016-03-01 00:00:00`
- `PageSize = 500`
- `PollingIntervalSeconds = 30`
- `MaxLagMinutes = 10`
- `SyncMode = StatusDriven`
- `StatusColumnName = TASKPROCESS`
- `PendingStatusValue = N`
- `CompletedStatusValue = Y`
- `ShouldWriteBackRemoteStatus = true`
- `SourceType = FullCase`
- `BusinessKeyColumn = SKUID`
- `BarcodeColumn = SKUID`
- `WaveCodeColumn = WAVENO`
- `WaveRemarkColumn = DESCR`

## 9.2 WmsFeedback 最终必须类似如下语义

- `SplitSchema = WMS_USER_431`
- `SplitTable = IDX_PICKTOLIGHT_CARTON1`
- `SplitBusinessKeyColumn = TASK_CODE`
- `FullCaseSchema = WMS_USER_431`
- `FullCaseTable = IDX_PICKTOWCS2`
- `FullCaseBusinessKeyColumn = TASK_CODE`

以及保留：
- `FeedbackStatusColumn`
- `FeedbackCompletedValue`
- `FeedbackTimeColumn`
- `ActualChuteColumn`
- `ScanTimeColumn`
- `LengthColumn`
- `WidthColumn`
- `HeightColumn`
- `VolumeColumn`
- `WeightColumn`
- `ScanCountColumn`
- `BusinessStatusColumn`
- `CommandTimeoutSeconds`

---

# 6. 全局禁止事项（必须原样执行）

1. 禁止本地继续保留 `IDX_PICKTOLIGHT_CARTON1`、`IDX_PICKTOWCS2`
2. 禁止 Oracle 源表先落本地镜像表再转 `business_tasks`
3. 禁止同步链路双写
4. 禁止查询链路双查
5. 禁止 `TargetChuteCode` 从远端表赋值
6. 禁止尺寸重量从远端表入站赋值
7. 禁止新增 `TaskCodeColumn`
8. 禁止新增 `TargetChuteColumn`
9. 禁止新增同步层的尺寸重量映射配置
10. 禁止保留 `IDX_SPLIT_TASK`
11. 禁止保留默认回写目标表
12. 禁止 SourceType 非法时回退默认表
13. 禁止退化查询性能
14. 禁止改变对外 API 契约
15. 禁止把业务投影逻辑塞进 Controller / Gateway / DbContext
16. 禁止留下 TODO / 临时兼容 / 以后再删

---

# 7. Copilot 自检门禁（每个 PR 完成后必须逐条输出结果）

## 架构自检
1. 本地是否只保留 `business_tasks` 作为业务主表？
2. 是否还存在本地 `IDX_PICKTOLIGHT_CARTON1`、`IDX_PICKTOWCS2` 的 DbSet、实体映射、快照？
3. 是否还存在镜像表双写、双查、兼容分支？
4. 业务投影逻辑是否只在 Application 服务中？
5. 是否保持 Host -> Infrastructure -> Application -> Domain 单向依赖？

## 配置自检
6. 是否保留并正确支持以下同步状态配置：
   - `SourceSchema`
   - `SourceTable`
   - `StatusColumnName`
   - `PendingStatusValue`
   - `CompletedStatusValue`
   - `ShouldWriteBackRemoteStatus`
   - `CursorColumn`
   - `StartTimeLocal`
   - `PageSize`
   - `PollingIntervalSeconds`
   - `MaxLagMinutes`
7. `PendingStatusValue = null` 时，是否生成 `IS NULL` 条件？
8. `CompletedStatusValue` 是否完全来自配置，而不是写死常量？
9. `ShouldWriteBackRemoteStatus=false` 时，是否真的不回写远端？
10. 是否已经删除：
   - `TargetChuteColumn`
   - `TaskCodeColumn`
   - 同步层尺寸重量映射配置？

## 业务能力自检
11. 扫描上传是否仍正常推进 `business_tasks.Status`？
12. 请求格口是否仍通过条码解析链路得到目标格口？
13. 落格回传是否仍正常更新 `ActualChuteCode` / `DroppedAtLocal`？
14. WMS 回写是否仍严格按 `SourceType` 分流到两张远端表？
15. 看板 / 报表 / 业务任务查询是否仍全部基于 `business_tasks`？

## 性能自检
16. 是否新增了任何全表拉取后内存过滤逻辑？
17. 投影 Upsert 是否走索引而不是全表扫描？
18. 查询服务是否仍是数据库侧过滤 + 分页？
19. `SourceTableCode + BusinessKey` 唯一索引是否存在？
20. 现有高频查询索引是否仍保留？

## 删除自检
21. 是否已经删除被覆盖的旧实现？
22. 是否还存在任何与本地镜像表有关的无用实体、配置、迁移、测试、DI 注册？
23. 是否还存在 `IDX_SPLIT_TASK`、默认回写表、默认兜底语义？
24. 是否还存在 TODO / 临时兼容 / 以后再删 的占位代码？

## 风险自检
25. 重复消费是否会产生重复 `business_tasks`？
26. 源端重复同步是否会覆盖本地运行态字段？
27. 删除旧镜像表后，启动、自动迁移、后台同步是否仍可运行？
28. `SourceType` 非法时是否会直接抛中文异常而不是回退默认表？

---

# 8. 最终交付要求（必须满足）

Copilot 最终交付的代码必须满足以下全部条件：

- 编译通过
- 测试通过
- 架构边界正确
- 不存在本地镜像表
- 不存在默认回写目标表
- 同步状态回写能力完整保留
- 业务结果回写能力完整保留
- 查询性能不退化
- 对外 API 契约不变
- 所有注释为中文
- 所有异常提示为中文
- 所有被覆盖旧实现全部删除

> 特别强调：  
> 任何新实现一旦覆盖旧实现，必须同步删除旧实现，不能“先留着以后再删”。
