# WMS回写联调基线

## 1. 文档目的
固化 WMS 回写上线前联调口径，明确“可联调、生产默认关闭”的当前状态，避免出现“代码可回写但启用条件不清晰”的风险。

## 2. 当前启用策略
- 当前策略：生产默认关闭，联调配置齐全。
- 当前配置位置：`EverydayChain.Hub.Host/appsettings.json` → `WmsFeedback.Enabled=false`。
- 联调环境策略：联调环境将 `Enabled` 设为 `true`，生产保持 `false`。
- 当前不能直接生产启用原因：
  1. 生产 Oracle 账号写权限尚未最终签核。
  2. 生产目标表触发器/行级规则副作用未完成最终验证。
  3. 生产变更窗口与回滚窗口未完成变更审批。
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
1. 生产数据库账号最终授权未完成。
2. 生产触发器副作用评估未签收。
3. 发布窗口与回滚脚本演练尚未完成。

## 10. 生产启用门禁
满足以下全部条件后才允许将生产 `WmsFeedback.Enabled` 置为 `true`：
1. 联调五项验证全部通过。
2. 权限、字段、触发器评估均通过并留痕。
3. 发布审批通过且回滚脚本已演练。
4. 发布后首轮监控指标（失败率、补偿量、行数一致性）正常。
