# EF Core 手动迁移操作指南（EverydayChain.Hub）

> 适用时间：2026-03-28 起。
> 适用项目：`EverydayChain.Hub.Infrastructure`（迁移生成） + `EverydayChain.Hub.Host`（启动项目）。

## 1. 前置条件
- 已安装 .NET 8 SDK。
- SQL Server 可连接。
- `EverydayChain.Hub.Host/appsettings.json` 中 `Sharding:ConnectionString` 已改为目标库连接串。

## 2. 安装 EF CLI（如未安装）
```bash
dotnet tool install --global dotnet-ef
```

## 3. 新增迁移（手动）
在仓库根目录执行：
```bash
dotnet ef migrations add <MigrationName> \
  --project EverydayChain.Hub.Infrastructure/EverydayChain.Hub.Infrastructure.csproj \
  --startup-project EverydayChain.Hub.Host/EverydayChain.Hub.Host.csproj \
  --context EverydayChain.Hub.Infrastructure.Persistence.HubDbContext \
  --output-dir Migrations
```

示例：
```bash
dotnet ef migrations add AddTraceExtraColumn \
  --project EverydayChain.Hub.Infrastructure/EverydayChain.Hub.Infrastructure.csproj \
  --startup-project EverydayChain.Hub.Host/EverydayChain.Hub.Host.csproj \
  --context EverydayChain.Hub.Infrastructure.Persistence.HubDbContext \
  --output-dir Migrations
```

## 4. 执行迁移到数据库
```bash
dotnet ef database update \
  --project EverydayChain.Hub.Infrastructure/EverydayChain.Hub.Infrastructure.csproj \
  --startup-project EverydayChain.Hub.Host/EverydayChain.Hub.Host.csproj \
  --context EverydayChain.Hub.Infrastructure.Persistence.HubDbContext
```

## 5. 导出 SQL 脚本（DBA 审核场景）
```bash
dotnet ef migrations script \
  --project EverydayChain.Hub.Infrastructure/EverydayChain.Hub.Infrastructure.csproj \
  --startup-project EverydayChain.Hub.Host/EverydayChain.Hub.Host.csproj \
  --context EverydayChain.Hub.Infrastructure.Persistence.HubDbContext \
  --idempotent \
  --output migration.sql
```

## 6. 回滚到指定迁移
```bash
dotnet ef database update <TargetMigration> \
  --project EverydayChain.Hub.Infrastructure/EverydayChain.Hub.Infrastructure.csproj \
  --startup-project EverydayChain.Hub.Host/EverydayChain.Hub.Host.csproj \
  --context EverydayChain.Hub.Infrastructure.Persistence.HubDbContext
```

回滚到初始空状态：
```bash
dotnet ef database update 0 \
  --project EverydayChain.Hub.Infrastructure/EverydayChain.Hub.Infrastructure.csproj \
  --startup-project EverydayChain.Hub.Host/EverydayChain.Hub.Host.csproj \
  --context EverydayChain.Hub.Infrastructure.Persistence.HubDbContext
```

## 7. 与自动迁移的关系
- 启动应用后，`AutoMigrationHostedService` 会自动执行 `Migrate()`。
- 手动迁移与自动迁移并不冲突：
  - 生产上可走“先脚本审核 + DBA 执行”；
  - 应用启动时会跳过已应用迁移，仅补齐未应用项。

## 8. 常见问题排查
- **报连接失败**：检查 `Sharding:ConnectionString` 是否可达。
- **报无法创建表**：确认数据库用户具有 DDL 权限。
- **模型与迁移不一致**：重新执行 `dotnet ef migrations add` 并检查 `ModelSnapshot`。
