# EverydayChain.Hub 详细业务背景开发指令（基于外部程序调用 API 的中控后端，不做 UI）

> 本文档用途：
>
> 1. 给 Copilot 作为**直接执行指令**
> 2. 补齐 Copilot 不知道的软件需求、业务链路、接口职责、状态语义
> 3. 明确当前项目**已经实现什么**、**不要重复实现什么**
> 4. 明确下一阶段只做哪些后端能力
>
> **重要约束**
>
> - 本项目当前阶段 **不做 UI**
> - 不做 Web 页面
> - 不做看板前端
> - 不做报表页面
> - 不做演示控制台流程
> - 不重写当前同步底座
> - 所有开发必须基于当前仓库现有结构继续演进
> - 若新增代码会覆盖原实现，必须明确指出需要删除或停用的旧代码
> - 注释全部中文
> - 异常提示全部中文
> - 本项目的扫描、请求格口、落格回传都**不是自动流程**
> - 本项目必须通过**对外暴露 API 接口**供其他程序调用
> - 当前至少需要对外暴露 **3 个 API 接口**
>   1. 扫描上传
>   2. 请求格口
>   3. 落格回传

---

# 1. 先给 Copilot 的业务背景（必须理解）

## 1.1 这个系统是什么

`EverydayChain.Hub` 不是前端系统，也不是单纯的同步脚本。  
它是一个**产线中控后端服务**，核心职责是：

1. 从 WMS 同步基础任务数据到本地
2. 对外暴露 API，供其他程序上传扫描条码
3. 根据本地业务任务和条码规则，解析并返回目标格口
4. 对外暴露 API，供其他程序在“实际落格完成后”回传结果
5. 按业务状态把必要结果回写本地，并在合适时机回写远端 WMS

---

## 1.2 这个项目当前不是“设备主动推流程”，而是“外部程序调用 API 驱动流程”

这是本次补充后必须修正的关键点。

### 错误理解（禁止）
- 扫描是自动进入本项目
- 本项目自动决定落格并自动写已落格
- 业务回传是本项目自动完成

### 正确理解（必须采用）
本项目是一个**被调用的业务中台**，由其他程序驱动：

#### 场景 A：扫描上传
其他程序调用本项目 API，把扫描条码、测量信息等传入。  
本项目只负责：

- 校验
- 解析
- 匹配本地任务
- 记录扫描信息
- 决定业务状态
- 在“请求格口接口”中返回目标格口

#### 场景 B：请求格口
其他程序调用本项目 API，传入条码或请求标识。  
本项目根据本地数据与业务规则返回：

- 是否命中任务
- 目标格口
- 是否异常
- 失败原因

#### 场景 C：落格回传
其他程序在“真正落格完成后”调用本项目 API。  
本项目根据回传结果：

- 更新本地任务状态为已落格
- 更新落格时间
- 更新业务日志
- 按规则决定是否进入后续业务回传

### 只有同步数据时的部分远端字段回写是自动的
这一点要和“业务回传”严格区分：

#### 自动回写（同步成功后）
当 Oracle -> 本地 同步成功后，可以自动回写远端部分字段，例如：

- `TASKPROCESS`
- `OPENTIME`

这属于**同步消费状态回写**。

#### 非自动回写（业务执行后）
例如“已落格”这类业务结果，不是同步时自动完成，而是必须等外部程序调用“落格回传 API”之后，由本项目更新本地业务状态，并再决定是否进行业务回传。

---

# 2. 软件需求中的真实业务链路（Copilot 必须按这个理解）

## 2.1 WMS 接单链路（自动后台同步）

### 步骤 1：WMS 在 Oracle 中间表写入待处理任务
WMS 在远端 Oracle 中间表提供拆零和整件任务数据。

### 步骤 2：Hub 后台自动同步到本地
`EverydayChain.Hub` 后台任务按配置定时同步：

- 有唯一键/业务键的表可以走 `KeyedMerge`
- 无主键/无唯一键/无稳定组合键的表可以走 `StatusDriven`

### 步骤 3：同步成功后自动回写远端部分字段
如果同步模式和配置允许，则同步成功后自动回写远端字段，例如：

- `TASKPROCESS`
- `OPENTIME`

> 注意：这一步是**同步层自动行为**，不是业务接口行为。

### 步骤 4：将同步记录转为本地业务任务
后续所有 API 链路都不应该直接围绕“同步原始表”工作，而应该围绕“本地统一业务任务”工作。

---

## 2.2 扫描上传链路（外部程序调用 API）

### 步骤 1：外部程序调用“扫描上传 API”
传入：

- 条码
- 扫描时间
- 设备编号
- 长宽高
- 体积
- 重量
- 追踪号/流水号（如有）

### 步骤 2：本项目进行业务解析
本项目完成：

1. 条码类型识别
2. 无效码过滤
3. 匹配本地任务
4. 更新扫描信息
5. 记录扫描日志

### 步骤 3：本项目不在这里直接认定“已落格”
扫描上传只表示“扫描发生了”，不表示“已经落格完成”。

---

## 2.3 请求格口链路（外部程序调用 API）

### 步骤 1：外部程序调用“请求格口 API”
传入条码或扫描关联标识。

### 步骤 2：本项目根据业务规则返回目标格口
本项目完成：

1. 查询本地任务
2. 判断任务是否有效
3. 判断是否异常
4. 返回目标格口

### 输出结果必须至少包含
- 是否成功
- 目标格口
- 业务任务 ID
- 是否异常
- 失败原因

---

## 2.4 落格回传链路（外部程序调用 API）

### 步骤 1：外部程序在真实落格完成后调用“落格回传 API”
传入：

- 业务任务标识
- 条码
- 格口
- 落格时间
- 结果状态
- 失败原因（可选）

### 步骤 2：本项目更新本地业务任务
更新：

- 已落格状态
- 落格时间
- 是否成功
- 错误原因
- 业务日志

### 步骤 3：业务回传不是自动扫描时完成，而是在落格后进入待回传状态
只有在落格完成后，任务才能被判定为进入“待业务回传”或“已完成”状态。

---

## 2.5 业务回传链路（由本项目后端执行，但基于业务状态，不是扫描时自动）

### 区分两类回写

#### A. 同步层自动回写
同步成功后，自动回写远端部分字段，例如：
- `TASKPROCESS`
- `OPENTIME`

#### B. 业务层回传
落格完成后，根据本地业务状态，回传真正的业务结果，例如：
- 已落格
- 落格时间
- 扫描次数
- 体积重量等（按后续规则分阶段实现）

> 业务回传不能等同于同步层自动回写。

---

# 3. 当前代码里已经实现的能力（严禁重复造轮子）

## 3.1 已存在的宿主与调度能力
当前已经有：

- `Program.cs`
- `SyncBackgroundWorker`
- `RetentionBackgroundWorker`

所以：
- **不要重新创建新的后台同步主宿主**
- **不要重新实现轮询框架**
- **不要重新实现全局定时器**

---

## 3.2 已存在的同步编排能力
当前已经有：

- `ISyncOrchestrator`
- `SyncOrchestrator`
- `ISyncExecutionService`
- `SyncExecutionService`

并且 `SyncExecutionService` 已支持：

- `KeyedMerge`
- `StatusDriven`

所以：
- **不要再新建另一套“总同步服务”**
- **不要再创建平行的第二套执行总入口**
- 所有后续同步相关新能力，都要基于当前执行链路扩展，不要分叉

---

## 3.3 已存在的源端读取能力
当前已经有：

- `OracleSourceReader`
- `OracleStatusDrivenSourceReader`

所以：
- **不要重新实现 Oracle 读取底座**
- **不要重复造新的 Oracle 通用查询器**

---

## 3.4 已存在的本地落地能力
当前已经有：

- `SyncStagingRepository`
- `SqlServerSyncUpsertRepository`
- `SqlServerAppendOnlyWriter`

所以：
- **不要再新造一套同步落库仓储**
- **不要重新实现 staging**
- **不要新建一个平行的 merge 仓储**

---

## 3.5 已存在的同步层远端回写能力
当前已经有：

- `OracleRemoteStatusWriter`
- `RemoteStatusConsumeService`

所以：
- **不要把“同步成功后回写 TASKPROCESS / OPENTIME 等字段”的能力再实现一遍**
- 这条链路属于**同步层自动回写**
- 后续新增的是**业务接口驱动的业务回传**

---

## 3.6 已存在的配置与检查点能力
当前已经有：

- `SyncTaskConfigRepository`
- `SyncCheckpointRepository`
- `SyncBatchRepository`
- `SyncChangeLogRepository`
- `SyncDeletionLogRepository`

所以：
- **不要重新实现检查点机制**
- **不要重新实现批次日志底座**

---

# 4. 当前项目下一阶段真正需要做的内容

> 下面这些才是当前仓库需要新增的业务能力。  
> Copilot 必须只做这些，不要重复造同步底座。

---

# 5. 第一阶段：统一业务任务模型（P0）

## 5.1 目标
建立“本地统一业务任务模型”，让后续 API 链路围绕统一业务任务执行。

---

## 5.2 必须新增的文件

### Domain 层
目录：

- `EverydayChain.Hub.Domain/BusinessTasks`

文件：

1. `BusinessTaskEntity.cs`
2. `BusinessTaskStatus.cs`
3. `BusinessTaskType.cs`
4. `BusinessTaskSourceType.cs`

### Application 层
目录：

- `EverydayChain.Hub.Application/BusinessTasks/Abstractions`
- `EverydayChain.Hub.Application/BusinessTasks/Services`

文件：

1. `IBusinessTaskMaterializer.cs`
2. `BusinessTaskMaterializer.cs`

---

## 5.3 设计要求

### `BusinessTaskEntity`
这是后续业务主实体，不是替代同步原表，而是 API 链路要围绕的统一任务模型。

建议字段至少包含：

- `Id`
- `SourceTableCode`
- `SourceRecordIdentity`
- `TaskType`
- `TaskStatus`
- `Barcode`
- `BoxCode`
- `OrderCode`
- `WaveCode`
- `StoreCode`
- `ChuteCode`
- `CreatedTimeLocal`
- `LastScannedTimeLocal`
- `DroppedTimeLocal`
- `ScanCount`
- `LengthMm`
- `WidthMm`
- `HeightMm`
- `VolumeMm3`
- `WeightGram`
- `IsRecirculated`
- `HasMultipleLabels`
- `IsDropCompleted`
- `IsFeedbackPending`
- `IsFeedbackCompleted`
- `FeedbackTimeLocal`
- `FailureReason`

### `BusinessTaskStatus`
至少包含：

- `Created`
- `Received`
- `Scanned`
- `ChuteRequested`
- `DropPending`
- `Dropped`
- `FeedbackPending`
- `FeedbackCompleted`
- `Exception`
- `Recirculated`
- `Cleaned`

### `BusinessTaskType`
至少包含：

- `Split`
- `Whole`

### `BusinessTaskSourceType`
至少包含：

- `WmsSplitPickToLightCarton`
- `WmsPickToWcs`

---

## 5.4 `BusinessTaskMaterializer` 职责

负责把同步过来的原始记录转换成本地业务任务。

### 输入
- 同步落地后的原始记录

### 输出
- `BusinessTaskEntity`

### 要求
- 只做转换与默认状态赋值
- 不做扫描业务
- 不做格口请求
- 不做落格回传

---

## 5.5 验收标准
1. 存在统一任务模型
2. 拆零/整件都能转换成本地任务
3. 后续 API 业务不再直接绑定同步原始表

---

# 6. 第二阶段：固化业务状态语义与接口文档（P0）

## 6.1 目标
让 Copilot 在不看软件需求原文的情况下，也知道关键状态和接口是什么意思。

---

## 6.2 必须新增的文档

放在仓库根目录：

1. `WMS状态语义基线.md`
2. `条码规则基线.md`
3. `对外API接口基线.md`
4. `拆零业务字段语义基线.md`
5. `整件业务字段语义基线.md`

---

## 6.3 `WMS状态语义基线.md` 必须写清楚

### 读取状态
- 字段：`TASKPROCESS`
- `N`：未读取
- `Y`：已读取

### 回传状态
- 字段：`STATUS`
- `N`：未回传
- `Y`：已回传

### 自动回写字段
同步成功后可自动回写远端的字段，例如：
- `TASKPROCESS`
- `OPENTIME`

### 业务回传字段
业务执行后再回传的字段，例如：
- 已落格状态
- 落格时间
- 扫描结果字段

---

## 6.4 `对外API接口基线.md` 必须写清楚

至少列出 3 个接口：

### 1）扫描上传 API
作用：
- 外部程序上传条码与测量信息
- 本项目解析并更新扫描状态

### 2）请求格口 API
作用：
- 外部程序请求目标格口
- 本项目根据本地任务返回目标格口

### 3）落格回传 API
作用：
- 外部程序在真实落格完成后回传
- 本项目更新任务为已落格

文档中每个接口都要写：

- 路由
- 方法
- 入参
- 出参
- 成功/失败语义
- 幂等要求
- 业务状态变化

---

# 7. 第三阶段：新增 3 个对外 API（P1）

## 7.1 目标
本项目至少需要对外暴露 3 个 API 接口。

---

## 7.2 API 层建议

当前项目是 Host + Application + Infrastructure 结构。  
如果要暴露 API，建议在 `Host` 中新增 Web API 能力，而不是另起新项目。

### 必须新增目录
- `EverydayChain.Hub.Host/Controllers`
- `EverydayChain.Hub.Host/Contracts/Requests`
- `EverydayChain.Hub.Host/Contracts/Responses`

---

## 7.3 必须新增的接口文件

### 扫描上传
1. `ScanUploadRequest.cs`
2. `ScanUploadResponse.cs`
3. `ScanController.cs`

### 请求格口
4. `ChuteRequestRequest.cs`
5. `ChuteRequestResponse.cs`

### 落格回传
6. `DropFeedbackRequest.cs`
7. `DropFeedbackResponse.cs`

---

## 7.4 接口定义要求

### API 1：扫描上传接口

#### 建议路由
`POST /api/tasks/scan-upload`

#### 入参建议
- `Barcode`
- `ScannedAtLocal`
- `DeviceCode`
- `LengthMm`
- `WidthMm`
- `HeightMm`
- `VolumeMm3`
- `WeightGram`
- `TraceId`

#### 返回建议
- `IsSuccess`
- `TaskId`
- `BarcodeType`
- `IsMatched`
- `FailureReason`

#### 职责
- 接收扫描信息
- 调用条码解析
- 调用本地任务匹配
- 更新扫描状态
- 写扫描日志

> 注意：扫描上传接口**不直接返回目标格口**，目标格口由“请求格口接口”返回。

---

### API 2：请求格口接口

#### 建议路由
`POST /api/tasks/request-chute`

#### 入参建议
- `Barcode`
- `TraceId`
- `RequestTimeLocal`

#### 返回建议
- `IsSuccess`
- `TaskId`
- `TargetChuteCode`
- `IsException`
- `FailureReason`

#### 职责
- 根据条码查找已扫描/有效任务
- 返回目标格口

> 注意：本接口的业务语义是“请求目标格口”，不是“确认已落格”。

---

### API 3：落格回传接口

#### 建议路由
`POST /api/tasks/drop-feedback`

#### 入参建议
- `TaskId`
- `Barcode`
- `ChuteCode`
- `DroppedAtLocal`
- `IsSuccess`
- `FailureReason`
- `TraceId`

#### 返回建议
- `IsSuccess`
- `TaskId`
- `TaskStatus`
- `FailureReason`

#### 职责
- 外部程序在真实落格完成后调用
- 更新本地任务状态为已落格或异常
- 写落格业务日志

---

## 7.5 Host 层改造要求

### Program.cs
需要把 Host 从纯 BackgroundService 宿主扩展为支持 API 的宿主。

#### 需要做的事
1. 注册 Controllers
2. 保留现有后台 Worker
3. 不移除现有同步后台任务
4. 允许 API 与 Worker 共存

### 注意
如果当前 `Program.cs` 仅按 Worker 方式启动，需要改造成：

- 支持 ASP.NET Core API
- 同时继续运行后台 Worker

### 如果新增实现会覆盖旧启动方式
必须明确指出需要删除的多余演示代码，但**不能删除现有同步 Worker**。

---

# 8. 第四阶段：扫描输入模型与条码解析（P1）

## 8.1 必须新增的文件

### Domain 层
目录：

- `EverydayChain.Hub.Domain/Barcodes`

文件：

1. `BarcodeType.cs`
2. `BarcodeParseResult.cs`

### Application 层
目录：

- `EverydayChain.Hub.Application/ScanProcessing/Models`
- `EverydayChain.Hub.Application/Barcodes/Abstractions`
- `EverydayChain.Hub.Application/Barcodes/Services`

文件：

1. `ScanEventArgs.cs`
2. `ScanMeasurementInfo.cs`
3. `IBarcodeParser.cs`
4. `BarcodeParser.cs`

---

## 8.2 职责

### `ScanEventArgs`
必须使用 `record class`，命名以 `EventArgs` 结尾。

### `BarcodeParser`
必须完成：
- 条码类型识别
- 无效码过滤
- 拆零/整件结构解析
- 输出标准化解析结果

---

# 9. 第五阶段：扫描匹配、请求格口与任务状态更新（P1）

## 9.1 必须新增的文件

### Application 层
目录：

- `EverydayChain.Hub.Application/ScanProcessing/Abstractions`
- `EverydayChain.Hub.Application/ScanProcessing/Services`
- `EverydayChain.Hub.Application/TaskExecution/Abstractions`
- `EverydayChain.Hub.Application/TaskExecution/Services`
- `EverydayChain.Hub.Application/Chutes/Abstractions`
- `EverydayChain.Hub.Application/Chutes/Services`

文件：

1. `IScanMatchService.cs`
2. `ScanMatchResult.cs`
3. `ScanMatchService.cs`
4. `ITaskExecutionService.cs`
5. `TaskExecutionService.cs`
6. `IChuteResolveService.cs`
7. `ChuteResolveService.cs`
8. `ChuteResolveResult.cs`

---

## 9.2 分工要求

### `ScanMatchService`
只负责：
- 根据解析结果查找本地任务
- 返回匹配结果

### `TaskExecutionService`
只负责：
- 更新扫描状态
- 更新扫描次数
- 更新测量数据
- 写扫描日志

### `ChuteResolveService`
只负责：
- 根据当前业务任务返回目标格口
- 不负责落格确认

---

# 10. 第六阶段：落格回传业务（P2）

## 10.1 目标
建立“真实落格完成后”的业务处理链路。

---

## 10.2 必须新增的文件

### Application 层
目录：

- `EverydayChain.Hub.Application/DropFeedback/Abstractions`
- `EverydayChain.Hub.Application/DropFeedback/Services`

文件：

1. `IDropFeedbackService.cs`
2. `DropFeedbackService.cs`

### Domain 层
目录：

- `EverydayChain.Hub.Domain/DropFeedback`

文件：

1. `DropFeedbackResult.cs`

---

## 10.3 职责

`DropFeedbackService` 必须完成：

1. 根据 `TaskId` 或条码找到任务
2. 校验回传格口是否合理
3. 更新任务为 `Dropped`
4. 更新落格时间
5. 写落格日志
6. 将任务置为 `FeedbackPending` 或 `FeedbackCompleted` 前置状态

---

# 11. 第七阶段：业务回传服务（P2）

## 11.1 目标
把“本地任务已落格后的业务结果回传”正式做出来。

> 注意：
>
> 当前项目已有“同步层自动回写 TASKPROCESS / OPENTIME”
>
> 这里新增的是：
>
> **落格完成后的业务回传**
>
> 两者不能混淆，也不能重复实现。

---

## 11.2 必须新增的文件

### Application 层
目录：

- `EverydayChain.Hub.Application/Feedback/Abstractions`
- `EverydayChain.Hub.Application/Feedback/Services`

文件：

1. `IWmsFeedbackService.cs`
2. `WmsFeedbackService.cs`

### Infrastructure 层
目录：

- `EverydayChain.Hub.Infrastructure/Feedback`

文件：

1. `OracleWmsFeedbackWriter.cs`

---

## 11.3 职责

### `WmsFeedbackService`
负责：
- 查询已落格且待业务回传任务
- 构建业务回传内容
- 调用 Oracle writer
- 更新本地回传状态

### `OracleWmsFeedbackWriter`
负责：
- 参数化回写 Oracle
- 只做基础持久化
- 中文日志
- 中文异常

---

# 12. 第八阶段：扫描日志与落格日志（P1/P2）

## 12.1 必须新增的文件

### Domain 层
目录：

- `EverydayChain.Hub.Domain/Aggregates/ScanLogAggregate`
- `EverydayChain.Hub.Domain/Aggregates/DropLogAggregate`

文件：

1. `ScanLogEntity.cs`
2. `DropLogEntity.cs`

### Infrastructure 层
目录：

- `EverydayChain.Hub.Infrastructure/Persistence/EntityConfigurations`

文件：

1. `ScanLogEntityTypeConfiguration.cs`
2. `DropLogEntityTypeConfiguration.cs`

---

## 12.2 要求
- 匹配成功和失败都要记扫描日志
- 落格成功和失败都要记落格日志

---

# 13. 第九阶段：异常规则（P2）

## 13.1 波次清理

### 文件
- `Application/WaveCleanup/Abstractions/IWaveCleanupService.cs`
- `Application/WaveCleanup/Services/WaveCleanupService.cs`

### 职责
- 按波次清理本地任务
- 记录清理日志
- 支持 dry-run

---

## 13.2 多标签规则

### 文件
- `Domain/MultiLabel/MultiLabelDecisionResult.cs`
- `Application/MultiLabel/Abstractions/IMultiLabelDecisionService.cs`
- `Application/MultiLabel/Services/MultiLabelDecisionService.cs`

### 职责
- 判断是否多标签
- 区分拆零/整件规则
- 输出处理决策

---

## 13.3 回流规则

### 文件
- `Domain/Recirculation/RecirculationDecisionResult.cs`
- `Application/Recirculation/Services/RecirculationService.cs`

### 职责
- 判断是否回流
- 更新任务回流状态

---

# 14. 第十阶段：补偿（P2）

## 14.1 必须新增文件

### Application 层
目录：

- `EverydayChain.Hub.Application/Compensation/Abstractions`
- `EverydayChain.Hub.Application/Compensation/Services`

文件：

1. `ICompensationService.cs`
2. `CompensationService.cs`

---

## 14.2 职责
- 识别业务回传失败任务
- 支持按任务重试
- 支持按批次重试
- 记录补偿日志

---

# 15. 明确禁止重复实现的内容

Copilot 必须遵守：

## 不要重复实现这些
1. 不要再新建一套“总同步服务”
2. 不要再新建一套 Oracle 通用读取器
3. 不要再新建一套 SQL Server merge 仓储
4. 不要再新建新的检查点机制
5. 不要再新建新的批次调度器
6. 不要把“同步层自动回写”再实现一遍
7. 不要把“业务回传”错误写成“同步成功后自动回写”
8. 不要把条码解析逻辑写进 Controller
9. 不要把格口解析逻辑写进 Controller
10. 不要把落格回传逻辑写进 Controller
11. Controller 只做入参校验和调用 Application Service
12. 不要写 UI

---

# 16. 输出要求（Copilot 完成每一阶段必须输出）

每完成一个阶段，必须说明：

1. 改了哪些文件
2. 每个文件放在哪一层
3. 该阶段解决了什么业务问题
4. 是否复用了现有同步底座
5. 是否覆盖了旧逻辑
6. 如果覆盖了旧逻辑，需要删除哪些多余代码
7. 当前阶段的验收标准是否满足

---

# 17. Copilot 可直接执行的短命令

```md
请基于当前 EverydayChain.Hub 项目继续开发，不要重写现有同步框架，不做 UI。

业务前提：
1. 本项目不是自动扫描系统，而是被其他程序调用的中控后端。
2. 本项目至少要对外暴露 3 个 API：
   - 扫描上传
   - 请求格口
   - 落格回传
3. 只有同步数据时，对远端部分字段（例如 TASKPROCESS、OPENTIME）的回写是自动的。
4. 业务回传不是自动的，必须等外部程序调用“落格回传 API”后才更新本地业务状态，并再进行业务回传。

当前项目已经有：
- Program.cs
- SyncBackgroundWorker / RetentionBackgroundWorker
- SyncOrchestrator
- SyncExecutionService
- OracleSourceReader
- OracleStatusDrivenSourceReader
- SqlServerSyncUpsertRepository
- RemoteStatusConsumeService
- SyncTaskConfigRepository
- SyncCheckpointRepository

这些能力不要重复实现。

下一阶段只做以下业务能力：

1. 新增本地统一业务任务模型：
   - Domain/BusinessTasks/BusinessTaskEntity.cs
   - Domain/BusinessTasks/BusinessTaskStatus.cs
   - Domain/BusinessTasks/BusinessTaskType.cs
   - Domain/BusinessTasks/BusinessTaskSourceType.cs

2. 新增业务任务转换服务：
   - Application/BusinessTasks/Abstractions/IBusinessTaskMaterializer.cs
   - Application/BusinessTasks/Services/BusinessTaskMaterializer.cs

3. 新增业务基线文档：
   - WMS状态语义基线.md
   - 条码规则基线.md
   - 对外API接口基线.md
   - 拆零业务字段语义基线.md
   - 整件业务字段语义基线.md

4. 在 Host 中新增 API 能力，并保留现有后台 Worker：
   - Host/Controllers/ScanController.cs
   - Host/Controllers/ChuteController.cs
   - Host/Controllers/DropFeedbackController.cs
   - Host/Contracts/Requests/*.cs
   - Host/Contracts/Responses/*.cs

5. 新增扫描输入与条码解析：
   - Application/ScanProcessing/Models/ScanEventArgs.cs
   - Application/ScanProcessing/Models/ScanMeasurementInfo.cs
   - Domain/Barcodes/BarcodeType.cs
   - Domain/Barcodes/BarcodeParseResult.cs
   - Application/Barcodes/Abstractions/IBarcodeParser.cs
   - Application/Barcodes/Services/BarcodeParser.cs

6. 新增扫描匹配、格口解析与任务执行：
   - Application/ScanProcessing/Abstractions/IScanMatchService.cs
   - Application/ScanProcessing/Models/ScanMatchResult.cs
   - Application/ScanProcessing/Services/ScanMatchService.cs
   - Application/TaskExecution/Abstractions/ITaskExecutionService.cs
   - Application/TaskExecution/Services/TaskExecutionService.cs
   - Application/Chutes/Abstractions/IChuteResolveService.cs
   - Application/Chutes/Services/ChuteResolveService.cs
   - Application/Chutes/Models/ChuteResolveResult.cs

7. 新增落格回传业务：
   - Application/DropFeedback/Abstractions/IDropFeedbackService.cs
   - Application/DropFeedback/Services/DropFeedbackService.cs
   - Domain/DropFeedback/DropFeedbackResult.cs

8. 新增业务回传服务（注意不是重复实现当前 StatusDriven 自动回写）：
   - Application/Feedback/Abstractions/IWmsFeedbackService.cs
   - Application/Feedback/Services/WmsFeedbackService.cs
   - Infrastructure/Feedback/OracleWmsFeedbackWriter.cs

9. 新增扫描日志与落格日志：
   - Domain/Aggregates/ScanLogAggregate/ScanLogEntity.cs
   - Domain/Aggregates/DropLogAggregate/DropLogEntity.cs
   - Infrastructure/Persistence/EntityConfigurations/ScanLogEntityTypeConfiguration.cs
   - Infrastructure/Persistence/EntityConfigurations/DropLogEntityTypeConfiguration.cs

10. 新增异常与补偿：
   - Application/WaveCleanup/Abstractions/IWaveCleanupService.cs
   - Application/WaveCleanup/Services/WaveCleanupService.cs
   - Domain/MultiLabel/MultiLabelDecisionResult.cs
   - Application/MultiLabel/Abstractions/IMultiLabelDecisionService.cs
   - Application/MultiLabel/Services/MultiLabelDecisionService.cs
   - Domain/Recirculation/RecirculationDecisionResult.cs
   - Application/Recirculation/Services/RecirculationService.cs
   - Application/Compensation/Abstractions/ICompensationService.cs
   - Application/Compensation/Services/CompensationService.cs

编码要求：
- 全部中文注释
- 全部中文异常
- 尽量使用 var
- enum 必须带 Description
- EventArgs 必须使用 record class 或 record struct
- 布尔字段必须用 Is/Has/Can/Should 前缀
- Controller 只做入参校验和调用 Application Service
- 不要重复造同步底座
- 如果会覆盖旧逻辑，明确指出需要删除哪些旧代码
```

---

# 18. 最后一句要求

Copilot 不要继续把项目当成“自动同步后自动执行所有业务”的程序。  
必须改按以下真实链路实现：

**后台自动同步接单 -> 生成本地业务任务 -> 外部程序调用扫描上传 API -> 外部程序调用请求格口 API -> 外部程序调用落格回传 API -> 本项目更新本地业务状态 -> 本项目执行业务回传/补偿**
