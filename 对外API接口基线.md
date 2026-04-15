# 对外API接口基线

## 1. 目标
- 固化扫描上传、请求格口、落格回传三类接口契约。
- 明确成功/失败语义、幂等要求、状态变化，约束后续实现。

## 2. 接口总览

| 接口 | 路由 | 方法 | 业务语义 |
|---|---|---|---|
| 扫描上传 | `/api/tasks/scan-upload` | `POST` | 接收扫描信息并推进扫描状态 |
| 请求格口 | `/api/tasks/request-chute` | `POST` | 查询并返回目标格口 |
| 落格回传 | `/api/tasks/drop-feedback` | `POST` | 回传真实落格结果并推进任务状态 |

> 路由命名口径：沿用需求文档的动作型路径，三类接口均采用“动作语义”命名。

## 3. 扫描上传 API

### 3.1 入参
- `Barcodes`（推荐，`List<string>`，可提交多个条码）
- `Barcode`（兼容，单条条码）
- `ScanTimeLocal`（必填，本地时间）
- `DeviceCode`（必填）
- `LengthMm`（可选）
- `WidthMm`（可选）
- `HeightMm`（可选）
- `VolumeMm3`（可选）
- `WeightGram`（可选）
- `TraceId`（建议）

> 说明：`Barcodes` 与 `Barcode` 至少提供其一；`Barcodes` 不允许空白条码，且单次最多 100 条；若为多条码场景，首条条码使用实测尺寸重量，其余条码尺寸重量按 `0` 回写，扫描时间保持一致。

### 3.2 出参
- `IsSuccess`
- `Data`（`List<ScanUploadResponse>`）
  - `IsAccepted`
  - `TaskCode`
  - `BarcodeType`
  - `FailureReason`

### 3.3 成功/失败语义
- 成功：扫描记录有效，任务匹配与状态推进成功。
- 失败：输入非法、条码不可解析、任务未命中或状态不允许推进。
- 条码解析固定规则：
  - 拆零：以 `02` 开头，格口号取第 3 位数字
  - 整件：以 `Z` 开头，格口号取第 2 位数字
  - 其余：不支持条码

### 3.4 幂等要求
- 同一业务请求重复提交不得产生重复脏写。
- 幂等键来源待确认（`TraceId` 或业务组合键）。

### 3.5 状态变化
- 典型变化：`Created -> Scanned`
- 禁止在本接口直接推进到 `Dropped`。

## 4. 请求格口 API

### 4.1 入参
- `Barcode`（必填）
- `TraceId`（建议）
- `RequestTimeLocal`（必填，本地时间）

### 4.2 出参
- `IsSuccess`
- `TaskId`
- `TargetChuteCode`
- `IsException`
- `FailureReason`

### 4.3 成功/失败语义
- 成功：命中有效任务并根据条码规则解析返回目标格口。
- 失败：任务不存在、任务状态非法或条码不满足“`02`/`Z` 标识 + 标识后首位数字格口号”规则。

### 4.4 幂等要求
- 同一任务重复请求应返回稳定格口决策结果（规则配置未变化前）。

### 4.5 状态变化
- 本接口为查询语义，不确认落格，不推进到 `Dropped`。

## 5. 落格回传 API

### 5.1 入参
- `TaskId`（必填之一，与 `Barcode` 至少提供一项）
- `Barcode`（必填之一，与 `TaskId` 至少提供一项）
- `ChuteCode`
- `DroppedAtLocal`（必填，本地时间）
- `IsSuccess`（必填）
- `FailureReason`（失败时）
- `TraceId`（建议）

### 5.1.1 双定位字段规则
- 仅提供 `TaskId`：按 `TaskId` 定位任务。
- 仅提供 `Barcode`：按 `Barcode` 定位任务。
- 同时提供 `TaskId` 与 `Barcode`：优先按 `TaskId` 定位，并校验 `Barcode` 必须与任务一致；不一致则返回失败（参数冲突）。

### 5.2 出参
- `IsSuccess`
- `TaskId`
- `TaskStatus`
- `FailureReason`

### 5.3 成功/失败语义
- 成功：任务落格结果已写入，本地状态推进完成。
- 失败：任务定位失败、状态机非法跳转、参数冲突或业务规则拒绝。
- 参数冲突：`TaskId` 与 `Barcode` 同时提供但不匹配。

### 5.4 幂等要求
- 同一落格事件重复回传时，必须可重入且不重复推进状态。

### 5.5 状态变化
- 成功：进入 `Dropped` 并可转 `FeedbackPending`。
- 失败：进入异常态并保留失败原因。

## 6. 统一约束
- Controller 仅负责参数校验与应用服务调用，不承载业务规则。
- 同步自动回写与业务回传严格分离。
- 所有时间参数采用本地时间语义，不使用 UTC 语义字段。

## 7. 待确认项
1. 三类 API 的认证方式（内网白名单、签名、Token）。
2. 幂等键最终口径（`TraceId`、设备请求号或业务组合键）。
3. 失败码字典是否需要统一对外枚举清单。
