#!/usr/bin/env bash
set -euo pipefail

# Linux systemd 安装脚本：
# - 将 EverydayChain Hub 注册为 systemd 服务
# - 默认服务名与值班手册保持一致：everydaychain-hub
# - 默认从同目录启动宿主程序，优先使用原生可执行文件，其次使用 dotnet + dll

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
service_name="${EVERYDAYCHAIN_SERVICE_NAME:-everydaychain-hub}"
service_description="${EVERYDAYCHAIN_SERVICE_DESCRIPTION:-EverydayChain Hub Host}"
service_user="${EVERYDAYCHAIN_SERVICE_USER:-root}"
service_group="${EVERYDAYCHAIN_SERVICE_GROUP:-${service_user}}"
service_unit_path="${EVERYDAYCHAIN_SERVICE_UNIT_PATH:-/etc/systemd/system/${service_name}.service}"
service_env_file="${EVERYDAYCHAIN_SERVICE_ENV_FILE:-/etc/default/${service_name}}"
working_directory="${EVERYDAYCHAIN_WORKING_DIRECTORY:-${script_dir}}"
host_binary_path="${script_dir}/EverydayChain.Hub.Host"
host_dll_path="${script_dir}/EverydayChain.Hub.Host.dll"
default_dotnet_path="${EVERYDAYCHAIN_DOTNET_PATH:-$(command -v dotnet 2>/dev/null || true)}"
dry_run=0
skip_start=0

require_root() {
    if [[ "${EUID}" -ne 0 ]]; then
        echo "[ERROR] Please run this script as root, or append --dry-run." >&2
        exit 1
    fi
}

ensure_systemctl() {
    if ! command -v systemctl >/dev/null 2>&1; then
        echo "[ERROR] systemctl is not available. This script only supports systemd-based Linux distributions." >&2
        exit 1
    fi
}

ensure_service_account() {
    if ! getent passwd "${service_user}" >/dev/null 2>&1; then
        echo "[ERROR] Service user does not exist: ${service_user}" >&2
        exit 1
    fi

    if ! getent group "${service_group}" >/dev/null 2>&1; then
        echo "[ERROR] Service group does not exist: ${service_group}" >&2
        exit 1
    fi
}

ensure_host_artifact() {
    if [[ -x "${host_binary_path}" ]]; then
        return 0
    fi

    if [[ -f "${host_dll_path}" ]]; then
        if [[ -n "${default_dotnet_path}" ]]; then
            return 0
        fi

        echo "[ERROR] Found ${host_dll_path}, but dotnet runtime was not found in PATH." >&2
        echo "        Set EVERYDAYCHAIN_DOTNET_PATH before re-running install.sh." >&2
        exit 1
    fi

    echo "[ERROR] Host entry point was not found in script directory." >&2
    echo "        Expected one of:" >&2
    echo "          - ${host_binary_path}" >&2
    echo "          - ${host_dll_path}" >&2
    exit 1
}

quote_for_shell() {
    printf '%q' "$1"
}

build_exec_start_command() {
    local escaped_working_directory
    local escaped_host_binary_path
    local escaped_host_dll_path
    local escaped_dotnet_path

    escaped_working_directory="$(quote_for_shell "${working_directory}")"
    escaped_host_binary_path="$(quote_for_shell "${host_binary_path}")"
    escaped_host_dll_path="$(quote_for_shell "${host_dll_path}")"
    escaped_dotnet_path="$(quote_for_shell "${default_dotnet_path:-/usr/bin/dotnet}")"

    printf "%s" "/bin/bash -lc 'set -euo pipefail; cd ${escaped_working_directory}; resolved_environment=\"\${EVERYDAYCHAIN_HUB_ENVIRONMENT:-}\"; if [[ -z \"\${resolved_environment}\" ]]; then if [[ -f appsettings.ReadOnlySync.json ]]; then resolved_environment=\"ReadOnlySync\"; else resolved_environment=\"Production\"; fi; fi; if [[ -x ${escaped_host_binary_path} ]]; then exec ${escaped_host_binary_path} --environment \"\${resolved_environment}\"; else exec \"\${EVERYDAYCHAIN_DOTNET_PATH:-${escaped_dotnet_path}}\" ${escaped_host_dll_path} --environment \"\${resolved_environment}\"; fi'"
}

ensure_environment_file() {
    local env_dir
    local legacy_default_content
    env_dir="$(dirname "${service_env_file}")"
    mkdir -p "${env_dir}"

    legacy_default_content="$(cat <<'EOF'
# EverydayChain Hub systemd environment file
# 宿主环境名：等价于命令行参数 --environment <name>
EVERYDAYCHAIN_HUB_ENVIRONMENT=Production

# 如需显式指定 dotnet 路径，可取消下一行注释并修改：
#EVERYDAYCHAIN_DOTNET_PATH=/usr/bin/dotnet
EOF
)"

    if [[ -f "${service_env_file}" ]]; then
        if [[ "$(cat "${service_env_file}")" == "${legacy_default_content}" ]]; then
            write_environment_file
            echo "[INFO] Migrated legacy environment file to auto-detect mode: ${service_env_file}"
        fi

        return 0
    fi

    write_environment_file
}

write_environment_file() {
    cat > "${service_env_file}" <<EOF
# EverydayChain Hub systemd environment file
# 默认自动判定：
# - 运行目录存在 appsettings.ReadOnlySync.json => ReadOnlySync
# - 否则 => Production
#
# 如需强制指定环境，可取消下一行注释并修改：
#EVERYDAYCHAIN_HUB_ENVIRONMENT=Production
#EVERYDAYCHAIN_HUB_ENVIRONMENT=ReadOnlySync

# 如需显式指定 dotnet 路径，可取消下一行注释并修改：
#EVERYDAYCHAIN_DOTNET_PATH=/usr/bin/dotnet
EOF
}

ensure_runtime_directories() {
    mkdir -p "${script_dir}/logs" "${script_dir}/data"
    chown -R "${service_user}:${service_group}" "${script_dir}/logs" "${script_dir}/data"
}

print_dry_run() {
    local exec_start_command
    exec_start_command="$(build_exec_start_command)"

    echo "[DryRun] service_name=${service_name}"
    echo "[DryRun] service_description=${service_description}"
    echo "[DryRun] service_user=${service_user}"
    echo "[DryRun] service_group=${service_group}"
    echo "[DryRun] service_unit_path=${service_unit_path}"
    echo "[DryRun] service_env_file=${service_env_file}"
    echo "[DryRun] working_directory=${working_directory}"
    echo "[DryRun] exec_start=${exec_start_command}"
    echo "[DryRun] create or keep environment file: ${service_env_file}"
    echo "[DryRun] ensure runtime directories: ${script_dir}/logs, ${script_dir}/data"
    echo "[DryRun] write systemd unit: ${service_unit_path}"
    echo "[DryRun] systemctl daemon-reload"
    echo "[DryRun] systemctl enable \"${service_name}\""
    if [[ "${skip_start}" == "1" ]]; then
        echo "[DryRun] skip service start"
    else
        echo "[DryRun] systemctl restart \"${service_name}\""
    fi
}

while (($# > 0)); do
    case "$1" in
        --dry-run)
            dry_run=1
            ;;
        --skip-start)
            skip_start=1
            ;;
        *)
            echo "[ERROR] Unsupported argument: $1" >&2
            exit 1
            ;;
    esac
    shift
done

ensure_systemctl
ensure_host_artifact

if [[ "${dry_run}" == "1" ]]; then
    print_dry_run
    exit 0
fi

require_root
ensure_service_account

chmod +x "${BASH_SOURCE[0]}"
if [[ -f "${script_dir}/uninstall.sh" ]]; then
    chmod +x "${script_dir}/uninstall.sh"
fi
if [[ -f "${host_binary_path}" ]]; then
    chmod +x "${host_binary_path}"
fi

ensure_environment_file
ensure_runtime_directories

if systemctl list-unit-files "${service_name}.service" >/dev/null 2>&1; then
    systemctl stop "${service_name}" >/dev/null 2>&1 || true
    systemctl disable "${service_name}" >/dev/null 2>&1 || true
fi

exec_start_command="$(build_exec_start_command)"
cat > "${service_unit_path}" <<EOF
[Unit]
Description=${service_description}
After=network-online.target
Wants=network-online.target
StartLimitIntervalSec=60
StartLimitBurst=3

[Service]
Type=simple
User=${service_user}
Group=${service_group}
WorkingDirectory=${working_directory}
EnvironmentFile=-${service_env_file}
ExecStart=${exec_start_command}
Restart=always
RestartSec=5
TimeoutStopSec=30
SyslogIdentifier=${service_name}

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "${service_name}"
systemctl reset-failed "${service_name}" >/dev/null 2>&1 || true

if [[ "${skip_start}" == "1" ]]; then
    echo "[INFO] Skipped service start."
else
    systemctl restart "${service_name}"
    echo "[INFO] Service restarted."
fi

cat <<EOF
[DONE] Install completed.
[INFO] ServiceName=${service_name}
[INFO] ServiceUnitPath=${service_unit_path}
[INFO] EnvironmentFile=${service_env_file}
[INFO] WorkingDirectory=${working_directory}
[INFO] Use 'systemctl status ${service_name}' to inspect service state.
EOF
