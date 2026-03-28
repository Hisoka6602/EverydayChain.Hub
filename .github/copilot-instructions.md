# Copilot 仓库强制执行规范

## 1. 目标与适用范围
- 本规范用于约束 Copilot 在本仓库内的所有修改行为。
- 适用于代码、配置、文档、CI 工作流与结构调整。

## 2. 时间处理硬性规则（禁止 UTC 语义）
- 全项目禁止使用 UTC 时间语义和 UTC 相关 API，包括但不限于：
  - `DateTime.UtcNow`
  - `DateTimeOffset.UtcNow`
  - `DateTimeKind.Utc`
  - `ToUniversalTime()`
  - `UtcDateTime`
  - `DateTimeStyles.AssumeUniversal`
  - `DateTimeStyles.AdjustToUniversal`
- 统一使用本地时间语义，包括但不限于：
  - `DateTime.Now`
  - `DateTimeKind.Local`
  - `DateTimeStyles.AssumeLocal`
- 读取配置中的时间字符串时，默认按本地时间解析。
- 示例配置不得使用 `Z` 或 offset（如 `+08:00`）。

## 3. 结构与命名规则
- 项目内所有代码文件的命名空间必须与物理目录层级严格一致。
- 每个类必须独立文件，禁止一个文件包含多个类。
- 禁止创建 `XxxManager / XxxHelper / XxxWrapper / XxxAdapter / XxxFacade`，除非可证明显著消除重复并降低复杂度。
- 禁止新增仅做单层转发的方法。
- 禁止保留旧实现再新增同义实现。

## 4. 注释与文档规则
- 所有方法必须有中文注释。
- 复杂实现方法必须包含步骤注释。
- 所有类字段必须有中文注释。
- 注释中禁止出现第二人称表述。
- Swagger 的参数、方法、枚举项必须有中文注释。
- 除 `README.md` 外，新增 md 文件命名需使用中文。

## 5. 分层与复用规则
- 严格划分结构层级边界，尽量保持 0 入侵。
- 禁止复制粘贴式重复代码；相同意义工具代码必须抽取集中。
- 小工具类需保持高性能、高复用与简洁性。

## 6. 领域规则
- 所有枚举必须定义在 `Zeye.Sorting.Hub.Domain.Enums` 的子目录下。
- 所有枚举必须包含注释与 `Description`。
- 所有事件载荷必须定义在 `Events` 子目录下。
- 事件载荷必须使用 `readonly record struct`。

## 7. 日志与异常规则
- 所有异常必须输出日志。
- 日志仅允许使用 NLog，且日志实现不得显著影响程序性能。

## 8. README 联动规则
- 每次新增或删除文件后，必须同步更新仓库根目录 `README.md`。
- 必须维护“各层级与各文件作用说明（逐项）”章节，使其与仓库实际结构一致。
- 新增/删除文件时，README 需同步包含“本次更新内容 / 后续可完善点”。
- README 中禁止写历史更新记录流水账。

## 9. 危险动作门禁
- 涉及数据库 DDL、批量删除、批量更新、外部调用重试策略等危险动作时，必须提供：
  - 开关控制
  - dry-run
  - 审计记录
  - 回滚脚本

## 10. CI 变更联动强制规则
- 当 `.github/copilot-instructions.md` 发生变更时，必须同步修改 CI 工作流文件（`.github/workflows/copilot-guard.yml`）。
- 未同步修改 CI 的 PR 必须失败。

## 11. 自检清单（Copilot 每次修改后执行）
- 检查是否引入 UTC 语义或 UTC API。
- 检查命名空间与目录是否一致。
- 检查新增/修改方法是否包含中文注释。
- 检查是否引入重复代码。
- 检查新增/删除文件是否同步更新 README 对应章节。
- 检查构建与测试是否通过；若存在历史阻塞，需明确说明阻塞项。

## 12. 执行要求
- Copilot 的答复、说明与交流使用中文。
- 若需求存在未确认项，先提出待确认项，未确认前不默认实现。
