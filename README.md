# EverydayChain.Hub

## 各层级与各文件作用说明（逐项）

### 根目录
- `EverydayChain.Hub.sln`：解决方案入口，聚合所有项目。
- `.gitattributes`：Git 属性配置。
- `.gitignore`：Git 忽略规则。
- `README.md`：仓库总览、结构职责说明与本次变更说明。

### `.github`
- `.github/copilot-instructions.md`：Copilot 强制执行规范（时间语义、结构规范、注释规范、自检与门禁要求）。
- `.github/workflows/copilot-guard.yml`：约束联动与基础静态门禁 CI（规则变更联动、UTC 禁用、配置时间样例检查）。

### `EverydayChain.Hub.Host`
- `Program.cs`：宿主启动入口，注册后台服务并启动 Host。
- `Worker.cs`：后台轮询任务实现，输出运行状态日志。
- `appsettings.json`：宿主通用配置。
- `appsettings.Development.json`：开发环境配置。
- `Properties/launchSettings.json`：本地启动配置。
- `EverydayChain.Hub.Host.csproj`：宿主项目文件。

### `EverydayChain.Hub.Domain`
- `EverydayChain.Hub.Domain.csproj`：领域层项目文件。
- `Abstractions/IEntity.cs`：实体抽象定义。
- `Aggregates/WmsPickToWcsAggregate/WmsPickToWcsEntity.cs`：WMS 下发至 WCS 分拣任务实体映射。
- `Aggregates/WmsSplitPickToLightCartonAggregate/WmsSplitPickToLightCartonEntity.cs`：WMS 下发至亮灯拆零箱任务实体映射。
- `Class1.cs`：领域层占位类型（待后续替换为业务类型）。

### `EverydayChain.Hub.Application`
- `EverydayChain.Hub.Application.csproj`：应用层项目文件。
- `Class1.cs`：应用层占位类型。

### `EverydayChain.Hub.Infrastructure`
- `EverydayChain.Hub.Infrastructure.csproj`：基础设施层项目文件。
- `Class1.cs`：基础设施层占位类型。

### `EverydayChain.Hub.SharedKernel`
- `EverydayChain.Hub.SharedKernel.csproj`：共享内核项目文件。
- `Class1.cs`：共享内核占位类型。

## 本次更新内容
- 新增 Copilot 规则文档 `.github/copilot-instructions.md`，将约束沉淀为仓库内可审计规范。
- 新增 CI 工作流 `.github/workflows/copilot-guard.yml`，实现规则变动联动与时间语义基础校验。
- 补充根目录 `README.md` 的逐文件职责说明。

## 后续可完善点
- 增加针对注释完整性、命名空间路径一致性的自动化校验脚本。
- 增加重复代码扫描门禁（按目录白名单逐步收敛误报）。
- 增加 NLog 专项门禁与性能基线检测。
