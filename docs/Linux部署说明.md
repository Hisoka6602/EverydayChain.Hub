# EverydayChain.Hub Linux 部署说明

本文档适用于使用 `systemd` 管理服务的 Linux 发行版，例如 Ubuntu、Debian、CentOS、Rocky Linux、Alibaba Cloud Linux。

## 1. 适用范围

- 已完成程序发布，发布目录内包含 `EverydayChain.Hub.Host.dll` 或原生可执行文件 `EverydayChain.Hub.Host`。
- 目标机器已安装 `systemd`。
- 以 `dotnet` 方式运行时，目标机器已安装 .NET 8 Runtime。
- 需要具备 `root` 或 `sudo` 权限，用于注册和卸载系统服务。

## 2. 发布目录要求

建议将以下文件一并部署到同一个目录，例如 `/opt/everydaychain-hub`：

- `EverydayChain.Hub.Host.dll` 或 `EverydayChain.Hub.Host`
- `appsettings.json`
- `appsettings.{Environment}.json`
- `nlog.config`
- `install.sh`
- `uninstall.sh`
- `logs/` 与 `data/` 目录可不预先创建，安装脚本会自动补齐

如果需要生成 Linux 发布目录，可在源码根目录执行：

```bash
dotnet publish EverydayChain.Hub.Host/EverydayChain.Hub.Host.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o ./publish/linux-x64
```

如果目标机器没有预装 .NET Runtime，也可以改为自包含发布：

```bash
dotnet publish EverydayChain.Hub.Host/EverydayChain.Hub.Host.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o ./publish/linux-x64
```

## 3. Linux 服务支持说明

项目已经内置 Linux 宿主支持：

- Windows 下自动注册为 Windows Service。
- Linux 下自动接入 `systemd`。
- `install.sh` / `uninstall.sh` 负责生成和移除 `systemd` 单元文件。

默认服务名为 `everydaychain-hub`，与现有值班手册中的运维命令保持一致。

## 4. 安装步骤

以下示例假设发布目录为 `/opt/everydaychain-hub`。

### 4.1 上传并进入发布目录

```bash
cd /opt/everydaychain-hub
```

### 4.2 授予脚本执行权限

```bash
chmod +x install.sh uninstall.sh
```

如果发布目录中包含原生可执行文件，也建议补一次执行权限：

```bash
chmod +x EverydayChain.Hub.Host || true
```

### 4.3 预演安装动作

先执行一次 Dry Run，确认服务名、工作目录、环境文件位置是否符合预期：

```bash
sudo ./install.sh --dry-run
```

### 4.4 正式安装

```bash
sudo ./install.sh
```

安装完成后，脚本会：

- 在 `/etc/systemd/system/everydaychain-hub.service` 生成服务单元
- 在 `/etc/default/everydaychain-hub` 生成环境文件
- 自动创建运行目录下的 `logs/`、`data/`
- 执行 `systemctl enable`
- 默认自动启动或重启服务

如果只想注册服务、暂时不启动：

```bash
sudo ./install.sh --skip-start
```

## 5. 常用自定义参数

安装脚本支持通过环境变量覆盖默认值：

```bash
sudo EVERYDAYCHAIN_SERVICE_NAME=everydaychain-hub \
     EVERYDAYCHAIN_SERVICE_USER=hub \
     EVERYDAYCHAIN_SERVICE_GROUP=hub \
     EVERYDAYCHAIN_WORKING_DIRECTORY=/opt/everydaychain-hub \
     EVERYDAYCHAIN_DOTNET_PATH=/usr/bin/dotnet \
     ./install.sh
```

常见变量说明：

- `EVERYDAYCHAIN_SERVICE_NAME`：服务名，默认 `everydaychain-hub`
- `EVERYDAYCHAIN_SERVICE_DESCRIPTION`：服务描述
- `EVERYDAYCHAIN_SERVICE_USER`：运行账号，默认 `root`
- `EVERYDAYCHAIN_SERVICE_GROUP`：运行用户组，默认与用户同名
- `EVERYDAYCHAIN_WORKING_DIRECTORY`：服务工作目录，默认脚本所在目录
- `EVERYDAYCHAIN_DOTNET_PATH`：`dotnet` 可执行文件路径
- `EVERYDAYCHAIN_SERVICE_UNIT_PATH`：`systemd` 单元文件路径
- `EVERYDAYCHAIN_SERVICE_ENV_FILE`：环境文件路径

## 6. 环境切换与配置文件

安装脚本生成的默认环境文件为：

```bash
/etc/default/everydaychain-hub
```

默认内容如下：

```bash
# 默认自动判定：
# - 运行目录存在 appsettings.ReadOnlySync.json => ReadOnlySync
# - 否则 => Production
#
# 如需强制指定环境，可取消下一行注释并修改：
#EVERYDAYCHAIN_HUB_ENVIRONMENT=Production
#EVERYDAYCHAIN_HUB_ENVIRONMENT=ReadOnlySync
#EVERYDAYCHAIN_DOTNET_PATH=/usr/bin/dotnet
```

说明：

- 默认情况下，脚本会在服务启动时自动判断：
  - 如果运行目录存在 `appsettings.ReadOnlySync.json`，则使用 `ReadOnlySync`
  - 如果运行目录不存在 `appsettings.ReadOnlySync.json`，则使用 `Production`
- 如果显式设置了 `EVERYDAYCHAIN_HUB_ENVIRONMENT`，则以该值为准，并覆盖自动判定逻辑。
- 当最终环境值为 `Production` 时，会加载 `appsettings.json` 与 `appsettings.Production.json`。
- 当最终环境值为 `ReadOnlySync` 时，会加载 `appsettings.json` 与 `appsettings.ReadOnlySync.json`。

如果需要强制指定只读联调环境，可以将环境文件修改为：

```bash
EVERYDAYCHAIN_HUB_ENVIRONMENT=ReadOnlySync
```

如果需要恢复为自动判定，只要把该行重新注释或删除即可。

修改后执行：

```bash
sudo systemctl restart everydaychain-hub
```

## 7. 启动后检查

安装完成后，建议按下面顺序检查：

### 7.1 查看服务状态

```bash
systemctl status everydaychain-hub --no-pager
```

### 7.2 跟踪运行日志

```bash
journalctl -u everydaychain-hub -f
```

### 7.3 查看应用日志目录

```bash
ls -lah /opt/everydaychain-hub/logs
```

应用日志默认写入发布目录下的 `logs/`，例如：

- `hub-YYYY-MM-DD.log`
- `sync-YYYY-MM-DD.log`
- `api-failure-YYYY-MM-DD.log`
- `nlog-internal.log`

## 8. 日志轮转与清理

当前版本已内置两层日志治理：

- `NLog` 负责文件归档与归档保留天数控制
- 应用内 `LogCleanup` 后台服务负责定期清理旧日志、统计日志目录大小，并在磁盘空间过低时告警

默认重点参数：

- `LogCleanup:Enabled = true`
- `LogCleanup:RetentionDays = 30`
- `LogCleanup:CheckIntervalHours = 6`
- `LogCleanup:StartupMinimumFreeSpaceMb = 200`

建议上线前确认：

- 发布目录所在磁盘剩余空间高于 200 MB
- `logs/archive/` 目录具备写权限
- 服务运行账号对发布目录、日志目录、数据目录具备读写权限

## 9. 常用运维命令

启动服务：

```bash
sudo systemctl start everydaychain-hub
```

停止服务：

```bash
sudo systemctl stop everydaychain-hub
```

重启服务：

```bash
sudo systemctl restart everydaychain-hub
```

查看最近 1 小时日志：

```bash
journalctl -u everydaychain-hub --since "1 hour ago"
```

查看开机自启状态：

```bash
systemctl is-enabled everydaychain-hub
```

## 10. 卸载步骤

执行：

```bash
sudo ./uninstall.sh
```

卸载脚本会：

- 停止服务
- 取消开机自启
- 删除 `systemd` 单元文件
- 执行 `daemon-reload`

卸载脚本不会删除以下内容：

- 发布目录
- `logs/`
- `data/`
- `/etc/default/everydaychain-hub`

如果需要彻底清理，请在确认无用后手动删除这些目录和文件。

## 11. 常见问题

### 11.1 `systemctl` 不存在

说明当前机器不是 `systemd` 体系，或未安装 `systemd`。当前脚本仅支持 `systemd` Linux。

### 11.2 提示找不到 `dotnet`

说明发布目录中只有 `dll`，但机器未安装 .NET Runtime，或者 `dotnet` 不在 PATH 中。可选处理方式：

- 安装 .NET 8 Runtime
- 设置 `EVERYDAYCHAIN_DOTNET_PATH=/usr/bin/dotnet`
- 改为自包含发布

### 11.3 服务能启动，但日志目录没有写入

通常是运行账号权限不足。请重点检查：

- 发布目录权限
- `logs/` 目录权限
- `data/` 目录权限
- `EVERYDAYCHAIN_SERVICE_USER` / `EVERYDAYCHAIN_SERVICE_GROUP` 配置是否正确

### 11.4 `ReadOnlySync` 自动判定没有生效

优先检查：

- 发布目录是否存在 `appsettings.ReadOnlySync.json`
- `/etc/default/everydaychain-hub` 中是否显式写死了 `EVERYDAYCHAIN_HUB_ENVIRONMENT`
- 修改后是否执行了 `systemctl restart everydaychain-hub`

## 12. 推荐上线检查清单

- 发布目录文件完整
- 环境文件内容正确
- `systemctl status everydaychain-hub` 正常
- `journalctl -u everydaychain-hub` 无启动异常
- `logs/` 正常产生日志
- API 预热与后台任务符合当前环境预期
- 磁盘剩余空间充足，满足启动最低空间门槛
