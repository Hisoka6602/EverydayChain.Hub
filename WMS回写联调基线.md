# WMS回写联调基线

## 1. 文档目的
本文档用于固化 PR-11 阶段 WMS 回写链路的联调口径，明确“当前可联调、生产默认关闭”的收口状态，避免回写配置再次回退到占位演示态。

## 2. 当前启用策略
1. 当前策略：生产默认关闭，联调配置齐全（方案 1）。
2. 配置项：`EverydayChain.Hub.Host/appsettings.json` → `WmsFeedback.Enabled = false`。
3. 联调环境启用方式：在联调环境配置中将 `WmsFeedback.Enabled` 置为 `true`，其余字段沿用本基线定义。

## 3. 目标表与业务键配置基线
| 来源类型 | Schema | Table | BusinessKeyColumn |
|---|---|---|---|
| 拆零（Split） | `WMS_USER_431` | `IDX_SPLIT_TASK` | `TASK_CODE` |
| 整件（FullCase） | `WMS_USER_431` | `IDX_FULLCASE_TASK` | `TASK_CODE` |

> 说明：`Schema/Table/BusinessKeyColumn` 默认值仅作为未知来源兜底，主链路以拆零/整件专用配置为准。

## 4. 回写字段映射基线
| 配置项 | 字段名 | 语义 |
|---|---|---|
| `FeedbackStatusColumn` | `FEEDBACK_STATUS` | 回传状态标记 |
| `FeedbackTimeColumn` | `FEEDBACK_TIME` | 回传时间 |
| `ActualChuteColumn` | `ACTUAL_CHUTE` | 实际格口 |
| `ScanTimeColumn` | `CLOSETIME` | 最后扫描时间 |
| `LengthColumn` | `LENGTH` | 长 |
| `WidthColumn` | `WIDTH` | 宽 |
| `HeightColumn` | `HIGH` | 高 |
| `VolumeColumn` | `CUBE` | 体积 |
| `WeightColumn` | `GROSSWEIGHT` | 重量 |
| `ScanCountColumn` | `SCANCOUNT` | 扫描次数 |
| `BusinessStatusColumn` | `STATUS` | 业务状态 |

## 5. 联调验证入口
1. 拆零任务回写验证：`WmsFeedbackServiceTests.ExecuteAsync_ShouldCompleteSplitTask_WhenSplitTaskWritten`
2. 整件任务回写验证：`WmsFeedbackServiceTests.ExecuteAsync_ShouldCompleteFullCaseTask_WhenFullCaseTaskWritten`
3. 批量回写验证：`WmsFeedbackServiceTests.ExecuteAsync_ShouldRespectBatchSize`
4. 回写失败补偿验证：`FeedbackCompensationServiceTests.RetryFailedBatchAsync_ShouldMarkCompleted_WhenGatewaySucceeds`
5. 行数不一致整批失败验证：`WmsFeedbackServiceTests.ExecuteAsync_ShouldMarkFailed_WhenWrittenRowsMismatch`

## 6. 仍需业务确认的阻塞项
1. 生产环境 Oracle 账号对 `IDX_SPLIT_TASK`、`IDX_FULLCASE_TASK` 的 `UPDATE` 权限最终确认。
2. 生产环境字段命名最终复核（重点：`HIGH`、`CUBE`、`GROSSWEIGHT`）与触发器副作用确认。
3. 生产启用窗口与回滚窗口最终审批。
