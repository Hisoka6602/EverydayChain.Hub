# EverydayChain.Hub

## 本次更新内容（2026-03-28）
- 已在本 PR 内完成 **分表自治**（按月分表自动路由 + 启动自建分表）。
- 已在本 PR 内完成 **自动迁移**（应用启动执行 EF Core `Migrate`，并预创建未来分表）。
- 已在本 PR 内完成 **自动调谐**（根据失败率与耗时动态调整批量写入窗口）。
- 已补充 **危险代码隔离器**（重试 + 超时 + 熔断）用于迁移与分表 DDL 等高风险操作。
- 已新增 **手动迁移文档**：`EFCore手动迁移操作指南.md`。
- 已新增 **Copilot 规则文件**：`.github/copilot-instructions.md`。
- 已新增 **治理 CI 工作流**：`.github/workflows/copilot-governance.yml`，用于规则变更联动与自动自检。

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
│   ├── Abstractions/IEntity.cs
│   ├── Aggregates/SortingTaskTraceAggregate/SortingTaskTraceEntity.cs
│   ├── Aggregates/WmsPickToWcsAggregate/WmsPickToWcsEntity.cs
│   └── Aggregates/WmsSplitPickToLightCartonAggregate/WmsSplitPickToLightCartonEntity.cs
├── EverydayChain.Hub.Infrastructure
│   ├── EverydayChain.Hub.Infrastructure.csproj
│   ├── DependencyInjection/ServiceCollectionExtensions.cs
│   ├── Options/ShardingOptions.cs
│   ├── Options/AutoTuneOptions.cs
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
│       ├── IShardTableManager.cs
│       ├── ShardTableManager.cs
│       ├── ISqlExecutionTuner.cs
│       ├── SqlExecutionTuner.cs
│       ├── ISortingTaskTraceWriter.cs
│       └── SortingTaskTraceWriter.cs
└── EverydayChain.Hub.Host
    ├── EverydayChain.Hub.Host.csproj
    ├── Program.cs
    ├── Worker.cs
    └── appsettings.json
```

## 各层级与各文件作用说明（逐项）
- `.github/copilot-instructions.md`：定义仓库级 Copilot 强制约束，覆盖时间语义、结构规范、文档联动与交付门禁。
- `.github/workflows/copilot-governance.yml`：执行规则自动校验，并强制规则文件与工作流联动修改。
- `SortingTaskTraceEntity.cs`：新增可分表的写入实体，用于承载中台追踪数据。
- `HubDbContext.cs`：根据分表后缀动态映射表名。
- `TableSuffixScope.cs` + `ShardModelCacheKeyFactory.cs`：保证不同后缀下 EF Model 能正确缓存隔离。
- `MonthShardSuffixResolver.cs`：按月份生成分表后缀（如 `_202603`）。
- `ShardTableManager.cs`：在 SQL Server 中自动创建分表与索引（不存在才建）。
- `AutoMigrationService.cs` + `AutoMigrationHostedService.cs`：应用启动时自动执行 `Migrate` 与分表预创建。
- `SqlExecutionTuner.cs`：基于失败率和耗时进行批量窗口升降调谐。
- `DangerZoneExecutor.cs`：危险路径统一走隔离器（超时/重试/熔断）。
- `SortingTaskTraceWriter.cs`：按分表后缀分组写入，并将执行结果回传给调谐器。
- `ServiceCollectionExtensions.cs`：统一注册基础设施依赖。
- `202603280001_InitialHubSchema.cs`：基础表结构迁移。
- `EFCore手动迁移操作指南.md`：提供手工迁移、脚本导出、回滚、排障流程。

## 可继续完善内容
- 将自动调谐状态持久化到 Redis 或配置中心，支持跨实例共享调谐窗口。
- 将分表策略扩展为按租户 + 月份的复合维度。
- 为 `SortingTaskTraceWriter` 增加幂等键（避免重复投递）。
- 增加集成测试：覆盖真实 SQL Server 容器下的迁移、分表创建、调谐验证。
- 将治理 CI 扩展为“每三次 PR 强制一次全量约束巡检”并落地自动计数机制。
