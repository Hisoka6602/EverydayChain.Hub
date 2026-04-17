# WMS回写联调基线

## 1. 文档目的
固化 WMS 回写上线前联调口径，明确“联调可用、生产已启用”的当前状态，避免出现“代码可回写但启用条件不清晰”的风险。

## 2. 当前启用策略
- 当前策略：生产与联调均启用。
- 本次上线最终口径：当前版本可上线，且 WMS 回写保持开启（`WmsFeedback.Enabled=true`）。
- 当前配置位置：`EverydayChain.Hub.Host/appsettings.json` → `WmsFeedback.Enabled=true`。
- 环境策略：生产与联调默认 `Enabled=true`，仅在应急止损时临时切换为 `false`。
- 当前生产启用前置确认：
  1. 生产 Oracle 账号写权限已签核。
  2. 生产目标表触发器/行级规则副作用已完成验证。
  3. 生产变更窗口与回滚窗口审批已完成。
- 确认责任：WMS 数据库管理员 + 分拣业务负责人 + 发布审批负责人。

## 3. 最终回写配置确认（基线值）
| 配置项 | 当前基线值 |
|---|---|
| SplitSchema | `WMS_USER_431` |
| SplitTable | `IDX_SPLIT_TASK` |
| FullCaseSchema | `WMS_USER_431` |
| FullCaseTable | `IDX_FULLCASE_TASK` |
| BusinessKeyColumn | `TASK_CODE` |
| SplitBusinessKeyColumn | `TASK_CODE` |
| FullCaseBusinessKeyColumn | `TASK_CODE` |
| FeedbackStatusColumn | `FEEDBACK_STATUS` |
| FeedbackTimeColumn | `FEEDBACK_TIME` |
| ActualChuteColumn | `ACTUAL_CHUTE` |
| ScanTimeColumn | `CLOSETIME` |
| LengthColumn | `LENGTH` |
| WidthColumn | `WIDTH` |
| HeightColumn | `HIGH` |
| VolumeColumn | `CUBE` |
| WeightColumn | `GROSSWEIGHT` |
| ScanCountColumn | `SCANCOUNT` |
| BusinessStatusColumn | `STATUS` |

## 4. 联调表、业务键与字段映射

### 4.1 联调目标表
| 来源类型 | Schema | Table | 业务键列 |
|---|---|---|---|
| 拆零（Split） | `WMS_USER_431` | `IDX_SPLIT_TASK` | `TASK_CODE` |
| 整件（FullCase） | `WMS_USER_431` | `IDX_FULLCASE_TASK` | `TASK_CODE` |

### 4.2 字段映射
| 本地业务字段 | 远端回写列 |
|---|---|
| FeedbackStatus | `FEEDBACK_STATUS` |
| FeedbackTimeLocal | `FEEDBACK_TIME` |
| ActualChuteCode | `ACTUAL_CHUTE` |
| ScannedAtLocal | `CLOSETIME` |
| LengthMm | `LENGTH` |
| WidthMm | `WIDTH` |
| HeightMm | `HIGH` |
| VolumeMm3 | `CUBE` |
| WeightGram | `GROSSWEIGHT` |
| ScanCount | `SCANCOUNT` |
| Status | `STATUS` |

## 5. 联调前准备
1. 联调库中存在 `IDX_SPLIT_TASK`、`IDX_FULLCASE_TASK` 且列定义与基线一致。
2. 联调账号具备目标表 `UPDATE` 权限。
3. 联调环境将 `WmsFeedback.Enabled` 设为 `true`。
4. 已准备可回放的拆零与整件任务样本。
5. 已开启业务日志落盘并可检索回写日志。

## 6. 联调步骤与入口
1. 拆零回写成功验证：`WmsFeedbackServiceTests.ExecuteAsync_ShouldCompleteSplitTask_WhenSplitTaskWritten`
2. 整件回写成功验证：`WmsFeedbackServiceTests.ExecuteAsync_ShouldCompleteFullCaseTask_WhenFullCaseTaskWritten`
3. 批量回写成功验证：`WmsFeedbackServiceTests.ExecuteAsync_ShouldRespectBatchSize`
4. 行数不一致整批失败验证：`WmsFeedbackServiceTests.ExecuteAsync_ShouldMarkFailed_WhenWrittenRowsMismatch`
5. 回写失败补偿验证：`FeedbackCompensationServiceTests.RetryFailedBatchAsync_ShouldMarkCompleted_WhenGatewaySucceeds`

## 7. 成功判定标准
1. Oracle 回写影响行数与待回写任务数一致。
2. 本地任务状态从 `Pending/Failed` 正确推进到 `Completed`。
3. 回写字段值与目标列映射一致。
4. 失败补偿可将失败任务重试成功并回填本地状态。

## 8. 失败排查方式
1. 检查 `WmsFeedback.Enabled` 与目标表配置是否为联调值。
2. 检查业务键字段是否与目标表实际列一致。
3. 检查 Oracle 权限、触发器、副作用约束。
4. 检查业务日志中 `RequestedCount`、`AffectedRows`、`WrittenRows`。
5. 行数不一致时优先排查触发器、过滤条件、业务键重复/缺失。

## 9. 当前阻塞项
当前无阻塞项。

## 10. 生产启用门禁
满足以下全部条件后才允许将生产 `WmsFeedback.Enabled` 置为 `true`：
1. 联调五项验证全部通过。
2. 权限、字段、触发器评估均通过并留痕。
3. 发布审批通过且回滚脚本已演练。
4. 发布后首轮监控指标（失败率、补偿量、行数一致性）正常。

## 11. 本次上线最终结论（收口）
### 11.1 方案 B（本次采用）
- 结论：当前版本可直接上线，且 WMS 回写开启。
- 配置要求：生产环境保持 `WmsFeedback.Enabled=true`。
- 影响说明：WMS 回写作为生产链路一部分随版本上线，需持续观测回写成功率与补偿量。

### 11.2 方案 A（应急回退）
- 如出现 Oracle 权限异常、触发器副作用或回写失败率异常抖升，可临时将 `WmsFeedback.Enabled` 调整为 `false`。
- 回退后需执行补偿与联调复核，满足“第 10 节生产启用门禁”后再恢复为 `true`。
