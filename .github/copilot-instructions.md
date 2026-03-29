# Copilot 仓库执行规范（强制）

## 1. 时间处理硬性规则
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
- 涉及时间配置字符串时，默认按本地时间解析；示例配置禁止使用 `Z` 和 offset（如 `+08:00`）。

## 2. 结构与命名规则
- 项目内所有代码文件的命名空间必须与物理目录层级严格一致。
- 每个类必须独立文件，禁止多类同文件。
- 所有枚举必须定义在 `EverydayChain.Hub.Domain.Enums` 子目录下。
- 所有枚举必须包含 XML 注释与 `Description`。
- 所有事件载荷必须定义在 `Events` 子目录下，并使用 `readonly record struct`。
- 字段、类型、文件、项目命名必须符合专业领域术语。
- 严格划分结构层级边界，保持低入侵实现。

## 3. 代码实现规则
- 所有方法必须有注释；复杂实现方法必须含步骤注释。
- 所有类字段必须有注释。
- 全局禁止复制粘贴式重复代码；发现同义工具代码需提取集中。
- 小工具类优先简洁、高性能、高复用。
- 可使用 `var` 的位置优先使用 `var`（不降低可读性前提下）。
- 禁止使用 `[Obsolete]` 维持旧实现；已过时代码必须删除并迁移到新实现。
- 禁止新增仅做一层转发的方法。
- 禁止创建 `XxxManager / XxxHelper / XxxWrapper / XxxAdapter / XxxFacade`，除非明确消除重复并降低复杂度。
- 禁止“复制一份再微调”支持多数据库。

## 4. 日志与异常规则
- 所有异常必须输出日志。
- 日志框架仅允许 `NLog`。
- 日志实现不得影响程序性能（高频路径需避免不必要开销）。

## 5. 文档与 README 联动规则
- 新增或删除文件后，必须同步更新仓库根目录 `README.md`：
  - 文件树
  - “各层级与各文件作用说明（逐项）”章节
  - “本次更新内容 / 后续可完善点”章节（用于当前迭代或当前 PR 摘要，不得长期累计为历史变更日志）
- 历史更新记录禁止长期写入 `README.md`，`README.md` 不作为跨版本 changelog 使用。
- 除 `README.md` 外，其他 Markdown 文件需使用中文命名；`.github` 目录下的治理类文档与工作流配套文档允许使用英文命名。
- 文档从 doc/pdf 解析到 md 的内容必须可追溯到原文出处。

## 6. Swagger 规则
- Swagger 的参数、方法、枚举项必须提供中文注释。

## 7. 危险动作门禁
- 涉及数据库 DDL、批量删除、批量更新、外部调用重试策略等危险动作，必须通过隔离器机制：
  - 开关控制
  - dry-run
  - 审计
  - 回滚脚本

## 8. PR 交付门禁
- 先输出“实施计划（Plan）”，再执行代码修改；每完成一步需更新进度。
- 改动文件必须通过编译；若无法编译，必须说明阻塞原因与替代验证。
- 每个 PR 必须附“验收清单（Checklist）”，逐项标注 `[x]/[ ]`。
- 每次 PR 都必须进行一次本文件约束违规检查并处理。
- 删除 `Oracle到SQLServer同步实施计划.md` 任一条目之前，必须先通读相关代码并确认“已完全实现 + 行为可用 + 验收通过”；未完全实现不得删除该条目。

## 9. CI 联动规则
- 必须存在 CI 对本规则文件进行自动校验。
- 当 `.github/copilot-instructions.md` 发生变更时，必须同步修改对应 CI 工作流文件；否则 CI 失败。
- CI 必须至少覆盖：
  - UTC 禁用 API 扫描
  - 命名空间与目录一致性扫描
  - README 联动更新检查（当新增/删除文件时）
