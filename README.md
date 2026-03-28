# EverydayChain.Hub

## 各层级与各文件作用说明（逐项）

- `EverydayChain.Hub.sln`：解决方案入口，统一组织各层项目。
- `EverydayChain.Hub.Host/`：宿主层，负责应用启动与运行时承载。
- `EverydayChain.Hub.Application/`：应用层，负责应用服务与用例编排。
- `EverydayChain.Hub.Domain/`：领域层，负责核心领域模型与业务规则。
- `EverydayChain.Hub.Infrastructure/`：基础设施层，负责外部依赖实现。
- `EverydayChain.Hub.SharedKernel/`：共享内核层，负责跨层复用的通用能力。
- `.github/copilot-instructions.md`：仓库级 Copilot 约束补充文件，定义强制执行规范。
- `README.md`：仓库总览与目录职责说明。

## 本次更新内容

- 新增 `.github/copilot-instructions.md`，补充并固化 Copilot 执行规则。
- 新增根目录 `README.md`，建立“各层级与各文件作用说明（逐项）”章节并同步文件职责。

## 后续可完善点

- 按当前规范补充更细粒度的项目内目录职责说明。
- 增加自动化校验流程，检查时间语义、注释与命名空间规则的合规性。
