# EverydayChain.Hub

## 本次更新内容（2026-03-28）
- 修复 `WmsSplitPickToLightCartonEntity.cs` 缺失命名空间闭合大括号导致的编译错误。
- 将 `ShardTableManager` / `IShardTableManager` 重命名为 `ShardTableProvisioner` / `IShardTableProvisioner`（遵守禁用 `XxxManager` 命名规范）。
- 引入 **NLog** 作为唯一日志实现（`NLog.Extensions.Logging` + `nlog.config`），并移除默认提供器。
- 修复 `nlog.config` 落盘隐患：`Microsoft.*` 过滤器改为 `final="true"`、File target 补充 `keepFileOpen`/`autoFlush`、`Program.cs` 增加 `LogManager.Shutdown()`。
- 为全部类、方法、字段、属性补充 XML 注释（满足"所有方法/字段必须有注释"规范）。
- 补充 `Microsoft.Extensions.Hosting.Abstractions` 与 `Microsoft.EntityFrameworkCore.Infrastructure` 显式引用，修复隐藏的编译依赖缺失问题。
- 新增 `DangerZoneOptions.cs`，将 `DangerZoneExecutor` 弹性策略参数（超时/重试/熔断）从硬编码迁移到可配置节点 `DangerZone`，补全配置覆盖面并为每个参数添加 XML 注释。
- `AutoTuneOptions` 新增 `SamplingWindowSize`（采样窗口大小）与 `FailureRateThreshold`（失败率阈值）两个可配置属性，消除 `SqlExecutionTuner` 中的硬编码魔法数字（原 `10` 与 `0.2`）。
- 新增 `WorkerOptions.cs`，将 `Worker` 后台轮询间隔从硬编码 `10` 秒迁移到 `appsettings.json` 的 `Worker.PollingIntervalSeconds` 节点；`Program.cs` 同步注册。

## 解决方案文件树与职责
```text
.
├── EverydayChain.Hub.sln
├── README.md
├── EFCore手动迁移操作指南.md
├── .github
│   ├── copilot-instructions.md
│   └── workflows
│       └── copilot-governance.yml
├── EverydayChain.Hub.Domain
│   ├── EverydayChain.Hub.Domain.csproj
│   ├── Class1.cs
│   ├── Abstractions/IEntity.cs
│   ├── Aggregates/SortingTaskTraceAggregate/SortingTaskTraceEntity.cs
│   ├── Aggregates/WmsPickToWcsAggregate/WmsPickToWcsEntity.cs
│   └── Aggregates/WmsSplitPickToLightCartonAggregate/WmsSplitPickToLightCartonEntity.cs
├── EverydayChain.Hub.Application
│   ├── EverydayChain.Hub.Application.csproj
│   └── Class1.cs
├── EverydayChain.Hub.SharedKernel
│   ├── EverydayChain.Hub.SharedKernel.csproj
│   └── Class1.cs
├── EverydayChain.Hub.Infrastructure
│   ├── EverydayChain.Hub.Infrastructure.csproj
│   ├── DependencyInjection/ServiceCollectionExtensions.cs
│   ├── Options/ShardingOptions.cs
│   ├── Options/AutoTuneOptions.cs
│   ├── Options/DangerZoneOptions.cs
│   ├── Persistence/HubDbContext.cs
│   ├── Persistence/DesignTimeHubDbContextFactory.cs
│   ├── Persistence/EntityConfigurations/SortingTaskTraceEntityTypeConfiguration.cs
│   ├── Persistence/Sharding/TableSuffixScope.cs
│   ├── Persistence/Sharding/IShardSuffixResolver.cs
│   ├── Persistence/Sharding/MonthShardSuffixResolver.cs
│   ├── Persistence/Sharding/ShardModelCacheKeyFactory.cs
│   ├── Migrations/202603280001_InitialHubSchema.cs
│   ├── Migrations/HubDbContextModelSnapshot.cs
│   └── Services
│       ├── IDangerZoneExecutor.cs
│       ├── DangerZoneExecutor.cs
│       ├── IAutoMigrationService.cs
│       ├── AutoMigrationService.cs
│       ├── AutoMigrationHostedService.cs
│       ├── IShardTableProvisioner.cs
│       ├── ShardTableProvisioner.cs
│       ├── ISqlExecutionTuner.cs
│       ├── SqlExecutionTuner.cs
│       ├── ISortingTaskTraceWriter.cs
│       └── SortingTaskTraceWriter.cs
└── EverydayChain.Hub.Host
    ├── EverydayChain.Hub.Host.csproj
    ├── Program.cs
    ├── Worker.cs
    ├── nlog.config
    ├── appsettings.json
    └── Options/WorkerOptions.cs
```

## 各层级与各文件作用说明（逐项）
- `.github/copilot-instructions.md`：定义仓库级 Copilot 强制约束，覆盖时间语义、结构规范、文档联动与交付门禁。
- `.github/workflows/copilot-governance.yml`：执行规则自动校验，并强制规则文件与工作流联动修改。
- `Class1.cs`（Domain/Application/SharedKernel）：各层占位类，后续实体/服务/工具请在对应子目录独立定义。
- `SortingTaskTraceEntity.cs`：可分表的写入实体，承载中台追踪数据；所有属性均含 XML 注释。
- `HubDbContext.cs`：根据分表后缀动态映射表名。
- `TableSuffixScope.cs` + `ShardModelCacheKeyFactory.cs`：保证不同后缀下 EF Model 能正确缓存隔离。
- `MonthShardSuffixResolver.cs`：按月份生成分表后缀（如 `_202603`）。
- `IShardTableProvisioner.cs` + `ShardTableProvisioner.cs`：在 SQL Server 中按需创建分表与索引（不存在才建），替代原 `ShardTableManager` 命名。
- `AutoMigrationService.cs` + `AutoMigrationHostedService.cs`：应用启动时自动执行 `Migrate` 与分表预创建。
- `SqlExecutionTuner.cs`：基于失败率和耗时进行批量窗口升降调谐；采样窗口大小与失败率阈值均来自 `AutoTuneOptions`。
- `DangerZoneExecutor.cs`：危险路径统一走隔离器（超时/重试/熔断），弹性参数来自 `DangerZoneOptions`。
- `DangerZoneOptions.cs`：`DangerZoneExecutor` 弹性策略配置类，绑定 `DangerZone` 节点，覆盖超时、重试、熔断全部参数，所有属性含 XML 注释。
- `SortingTaskTraceWriter.cs`：按分表后缀分组写入，并将执行结果回传给调谐器。
- `ServiceCollectionExtensions.cs`：统一注册基础设施依赖。
- `202603280001_InitialHubSchema.cs`：基础表结构迁移。
- `nlog.config`：NLog 日志配置，输出至控制台与滚动日志文件（按日切割，保留 30 天）。
- `WorkerOptions.cs`：后台工作服务配置类，绑定 `Worker` 节点，覆盖轮询间隔（`PollingIntervalSeconds`），含 XML 注释。
- `EFCore手动迁移操作指南.md`：提供手工迁移、脚本导出、回滚、排障流程。

## 可继续完善内容
- 将自动调谐状态持久化到 Redis 或配置中心，支持跨实例共享调谐窗口。
- 将分表策略扩展为按租户 + 月份的复合维度。
- 为 `SortingTaskTraceWriter` 增加幂等键（避免重复投递）。
- 增加集成测试：覆盖真实 SQL Server 容器下的迁移、分表创建、调谐验证。
- 将治理 CI 扩展为"每三次 PR 强制一次全量约束巡检"并落地自动计数机制。
