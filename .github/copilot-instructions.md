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
- 所有配置实体类必须定义在 `EverydayChain.Hub.Domain.Options` 子目录下。
- 聚合根只能定义在 `Aggregates` 子目录下。
- 所有事件必须定义在 `EverydayChain.Hub.Domain.Events` 子目录下。
- 静态工具类只能定义在 `EverydayChain.Hub.SharedKernel.Utilities` 子目录下。
- 所有枚举必须包含 XML 注释与 `Description`。
- 所有事件载荷必须定义在 `Events` 子目录下，并使用 `readonly record struct`。
- 字段、类型、文件、项目命名必须符合专业领域术语。
- 严格划分结构层级边界，保持低入侵实现。
- Host 层禁止使用 `Servers` 目录命名，统一使用 `Services`。

## 3. 代码实现规则
- 所有方法必须有注释；复杂实现方法必须含步骤注释。
- 所有类字段必须有注释。
- 所有配置项必须有中文注释（至少体现在配置类属性 XML 注释或配套文档中，并在 Swagger 中补齐配置参数中文说明）。
- 配置文件（如 `appsettings*.json`）的每一项都需要注释，注释方式参考 `https://github.com/Hisoka6602/Zeye.NarrowBeltSorter/blob/master/Zeye.NarrowBeltSorter.Host/appsettings.json`（JSON 注释风格）。
- 若配置项为集合类型（数组），至少需要提供一个示例元素。
- 全局禁止复制粘贴式重复代码；发现同义工具代码需提取集中。
- 小工具类优先简洁、高性能、高复用。
- 可使用 `var` 的位置优先使用 `var`（不降低可读性前提下）。
- 禁止使用 `[Obsolete]` 维持旧实现；已过时代码必须删除并迁移到新实现。
- 禁止新增仅做一层转发的方法。
- 禁止创建 `XxxManager / XxxHelper / XxxWrapper / XxxAdapter / XxxFacade`，除非明确消除重复并降低复杂度。
- 禁止“复制一份再微调”支持多数据库。
- 本项目不可以有任何的脱敏操作
- 整个项目只能有一个安全执行器，通过现有依赖注入单例统一访问，不得新增并行实现。
- 禁止在热路径读写配置文件和数据库
- 每个配置项的注释都需要写明可填写的范围，枚举类型需要列出所有枚举项
- 重试策略需要使用 `Polly` 库。
- C# 代码注释（包括 `//`、`/* */` 与 XML 文档注释）中禁止出现第二人称字眼（如“你/你们”）；统一使用第三人称或客观描述（如“该方法/此参数”）。
- 需求不明确时，必须先列出“待确认项”并记录在 PR 描述或 Issue；未确认项不得默认实现。
- 在不降低可读性和可维护性的前提下，优先采用更高性能实现；热路径需避免重复遍历、循环内重复分配与 N+1 查询。
- 禁止脱敏；仓库内仅允许密钥/密码/连接参数的占位符或示例值，真实密钥必须通过环境变量、密钥服务或其他安全的 Secret 管理机制注入

## 4. 日志与异常规则
- 所有异常必须输出日志。
- 日志框架仅允许 `NLog`。
- 日志实现不得影响程序性能（高频路径需避免不必要开销）。
- 强制：所有业务日志必须落盘到文件，不允许仅输出到控制台；新增日志分类时必须在 NLog 配置中声明对应文件 target 与路由规则
- 单个日志文件大小上限为 10 MB；超过后必须触发轮转（NLog `archiveAboveSize` 不得超过 10485760 字节）。

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
- 删除 `Oracle到SQLServer同步实施计划.md` 任一条目之前，必须先通读相关代码并确认“已完全实现 + 行为可用 + 验收通过”；未完全实现不得删除该条目。（该文档是当前 Oracle→SQLServer 同步专项实施清单，按此专项强制执行）

## 9. CI 联动规则
- 必须存在 CI 对本规则文件进行自动校验。
- 当 `.github/copilot-instructions.md` 发生变更时，必须同步修改对应 CI 工作流文件；否则 CI 失败。
- CI 必须至少覆盖：
  - UTC 禁用 API 扫描
  - 命名空间与目录一致性扫描
  - README 联动更新检查（当新增/删除文件时）
  - 配置文件逐项注释完整性检查
 
    
# 10. Copilot交互
- 所有Copilot的任务名称、提示、问答、描述都需要使用中文
