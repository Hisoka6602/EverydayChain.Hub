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
- 自动迁移与分表建表必须按“每个逻辑表对应实体模型”生成字段与索引，禁止复用其他实体模板创建异构业务表结构。
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

## 11. DDD 分层接口与实现放置规则（细化强制）
- 分层依赖方向必须保持：`Host -> Infrastructure -> Application -> Domain`。
- `Domain` 禁止依赖 `Application`、`Infrastructure`、`Host`；`Application` 禁止依赖 `Infrastructure`、`Host`。
- 接口归属必须按语义决定，不允许按实现便利性决定。
- 表达领域能力边界的抽象（领域服务/策略/规格/工厂等）必须定义在 `Domain`。
- 表达应用编排与外部协作能力的抽象必须定义在 `Application/Abstractions/*`。
- 表达基础设施内部技术细节（协议编解码、CRC、通信细节）的抽象只能定义在 `Infrastructure` 内部。
- 当前项目持久化协作抽象统一定义在 `EverydayChain.Hub.Application/Abstractions/Persistence`，禁止新增到 `EverydayChain.Hub.Application/Repositories`。
- `Host` 仅允许承载启动、DI 组装、后台任务入口、控制器/Hub/中间件；禁止承载仓储实现、网关实现、驱动实现、协议编解码实现。
- `Domain` 抽象的外部资源实现必须落在 `Infrastructure`；`Application` 抽象的外部资源实现必须落在 `Infrastructure`。
- 禁止将基础设施实现细节（EF/SQL/Redis/HttpClient/文件系统/驱动协议）泄漏到 `Domain` 或 `Application` 业务抽象中。
- 同一职责禁止重复抽象与重复实现；迁移时必须同步删除旧接口、旧实现、旧 DI 注册与旧调用。
- 禁止新增仅做一层透传的服务/仓储实现；已有透传路径必须优先合并到现有实现以降低复杂度。
- 命名规则强制执行：
  - Repository：`I{Name}Repository` / `{Name}Repository`
  - Query/Read：`I{Name}QueryService`、`I{Name}ReadService`
  - Gateway/Client：`I{Name}Gateway`、`I{Name}Client`
  - Domain Policy/Specification/Strategy：`I{Name}Policy`、`I{Name}Specification`、`I{Name}Strategy`
  - Factory：`I{Name}Factory`
  - 协议编解码：`I{Name}FrameCodec`、`I{Name}ProtocolParser`
- 新增抽象与实现时，必须在 PR 描述明确标注其所在层级与物理目录，确保审查可追溯。
- 接口分类与放置细化：
  - 领域仓储接口（围绕聚合与领域持久化边界）定义在 `Domain/Repositories`，实现放在 `Infrastructure/Persistence/Repositories`。
  - 应用层持久化协作抽象（同步链路编排所需读取/写入/审计/检查点契约）统一定义在 `Application/Abstractions/Persistence`，实现放在 `Infrastructure/Repositories`。
  - 领域服务接口定义在 `Domain/Services`；纯规则实现在 `Domain`，外部资源依赖实现放在 `Infrastructure`。
  - 领域策略/规格/规则接口定义在 `Domain/Policies`、`Domain/Specifications`，禁止放到 `Application`。
  - 领域工厂接口定义在 `Domain/Factories`，用于保障聚合创建不变式。
  - `IUnitOfWork` 必须统一定义在 `Application/Abstractions/Persistence`，实现放在 `Infrastructure`，禁止多处重复定义。
  - 查询/读模型接口必须定义在 `Application/Abstractions/Queries`，命名使用 `I{Name}QueryService` 或 `I{Name}ReadService`。
  - 当前用户/租户/权限等上下文接口定义在 `Application/Abstractions/Security`，实现放在 `Infrastructure/Security`。
  - 本地化接口定义在 `Application/Abstractions/Localization`，实现放在 `Infrastructure/Localization`。
  - 文件存储/导入导出接口定义在 `Application/Abstractions/Storage|Import|Export`，实现放在 `Infrastructure/Storage|Import|Export`。
  - 消息发布/总线接口定义在 `Application/Abstractions/Messaging`，实现放在 `Infrastructure/Messaging`。
  - 第三方系统网关/客户端接口定义在 `Application/Abstractions/Integrations`，实现放在 `Infrastructure/Integrations`。
  - 业务设备能力抽象可定义在 `Application/Abstractions/Devices`，实现放在 `Infrastructure/Devices`。
  - 协议编解码/CRC/报文解析接口只能放在 `Infrastructure/Devices/Protocols/*`，禁止上浮到 `Application` 或 `Domain`。
- 实现放置细化：
  - `Domain` 允许：领域规则实现、领域服务实现、规格实现、工厂实现、值对象行为。
  - `Domain` 禁止：`DbContext`、EF 配置、SQL、Redis、`HttpClient`、文件系统、MQ、驱动协议实现。
  - `Application` 允许：应用服务、命令/查询处理器、用例编排、DTO 映射等纯应用逻辑。
  - `Application` 禁止：仓储实现、`DbContext`、SQL 实现、`HttpClient` 实现、Redis 实现、驱动实现、协议编解码实现。
  - `Infrastructure` 允许：仓储实现、`DbContext`、UnitOfWork、EF 配置、网关实现、缓存实现、文件存储实现、设备驱动、通信适配、协议编解码。
  - `Host` 只允许：`Program`、Controller、Hub、HostedService 入口、中间件、DI 组装、配置绑定。
- 目录与迁移门禁细化：
  - 禁止在 `Application` 新增 `Repositories` 目录承载抽象；必须使用 `Abstractions` 子目录按语义分类。
  - 迁移过程中禁止新旧目录并存，必须一次性完成接口迁移、实现引用替换、DI 注册替换、旧文件删除。
  - 若发现同职责重复接口（如 `IOrderRepo` 与 `IOrderRepository`），必须保留一套标准命名并删除另一套。
  - 任何 PR 新增抽象时，必须在描述中声明“定义层目录 + 实现层目录 + 命名符合项”。
