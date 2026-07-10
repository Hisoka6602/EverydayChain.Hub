#!/usr/bin/env bash
set -euo pipefail

# Linux systemd 卸载脚本：
# - 停止并删除 systemd 服务
# - 保留发布目录、日志目录和环境文件，避免误删用户数据

service_name="${EVERYDAYCHAIN_SERVICE_NAME:-everydaychain-hub}"
service_unit_path="${EVERYDAYCHAIN_SERVICE_UNIT_PATH:-/etc/systemd/system/${service_name}.service}"
service_env_file="${EVERYDAYCHAIN_SERVICE_ENV_FILE:-/etc/default/${service_name}}"
dry_run=0

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

print_dry_run() {
    echo "[预演模式] service_name=${service_name}"
    echo "[预演模式] service_unit_path=${service_unit_path}"
    echo "[预演模式] 执行 systemctl stop \"${service_name}\""
    echo "[预演模式] 执行 systemctl disable \"${service_name}\""
    echo "[预演模式] 删除 systemd 单元文件：${service_unit_path}"
    echo "[预演模式] 执行 systemctl daemon-reload"
    echo "[预演模式] 执行 systemctl reset-failed \"${service_name}\""
    echo "[预演模式] 保留环境变量文件：${service_env_file}"
}

while (($# > 0)); do
    case "$1" in
        --dry-run)
            dry_run=1
            ;;
        *)
            echo "[错误] 不支持的参数：$1" >&2
            exit 1
            ;;
    esac
    shift
done

ensure_systemctl

if [[ "${dry_run}" == "1" ]]; then
    print_dry_run
    exit 0
fi

require_root

if systemctl list-unit-files "${service_name}.service" >/dev/null 2>&1; then
    systemctl stop "${service_name}" >/dev/null 2>&1 || true
    systemctl disable "${service_name}" >/dev/null 2>&1 || true
fi

if [[ -f "${service_unit_path}" ]]; then
    rm -f "${service_unit_path}"
fi

systemctl daemon-reload
systemctl reset-failed "${service_name}" >/dev/null 2>&1 || true

cat <<EOF
[DONE] Uninstall completed.
[INFO] ServiceName=${service_name}
[INFO] ServiceUnitPath=${service_unit_path}
[INFO] EnvironmentFileKept=${service_env_file}
[INFO] Application files and logs were not removed.
EOF
