#!/usr/bin/env bash
# =============================================================================
# EverydayChain.Hub 一键体检脚本
# 用途：快速检查磁盘、配置、关键文件可读写性、进程存活与日志健康状态。
# 用法：bash scripts/health-check.sh [数据目录路径] [日志目录路径]
#
# 参数（可选，默认值如下）：
#   $1  数据目录路径（默认：脚本同级目录下的 data/）
#   $2  日志目录路径（默认：脚本同级目录下的 logs/）
#
# 退出码：
#   0   所有检查通过
#   1   存在一项或多项检查未通过
# =============================================================================

set -euo pipefail

# -----------------------------------------------------------------------
# 基础路径配置
# -----------------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BASE_DIR="$(dirname "$SCRIPT_DIR")"
DATA_DIR="${1:-$BASE_DIR/data}"
LOG_DIR="${2:-$BASE_DIR/logs}"

CHECKPOINT_FILE="$DATA_DIR/sync-checkpoints.json"
TARGET_STORE_PATTERN="$DATA_DIR/sync-target-store.*.json"
APPSETTINGS_FILE="$BASE_DIR/EverydayChain.Hub.Host/appsettings.json"
NLOG_CONFIG_FILE="$BASE_DIR/EverydayChain.Hub.Host/nlog.config"

# 最小可用磁盘空间（MB）
MIN_FREE_DISK_MB=500
# 日志文件超过此大小（MB）时告警
LOG_FILE_WARN_MB=100
# 最近多少小时内应有日志写入（否则告警进程可能卡死）
LOG_ACTIVE_HOURS=2

PASS=0
FAIL=0

# -----------------------------------------------------------------------
# 工具函数
# -----------------------------------------------------------------------
green() { echo -e "\033[32m$*\033[0m"; }
red()   { echo -e "\033[31m$*\033[0m"; }
yellow(){ echo -e "\033[33m$*\033[0m"; }

ok()   { green "  [PASS] $*"; ((PASS++)); }
fail() { red   "  [FAIL] $*"; ((FAIL++)); }
warn() { yellow "  [WARN] $*"; }
header() { echo; echo "=== $* ==="; }

# -----------------------------------------------------------------------
# 1. 磁盘空间检查
# -----------------------------------------------------------------------
header "磁盘空间检查"

check_disk_space() {
    local path="$1"
    local min_mb="$2"
    if [ ! -e "$path" ]; then
        # 路径不存在时检查上级目录
        path="$(dirname "$path")"
    fi
    if [ ! -e "$path" ]; then
        warn "路径不存在，跳过磁盘空间检查：$path"
        return
    fi
    local free_kb
    free_kb=$(df -k "$path" | awk 'NR==2{print $4}')
    local free_mb=$((free_kb / 1024))
    if [ "$free_mb" -ge "$min_mb" ]; then
        ok "磁盘可用空间充足（${free_mb} MB >= ${min_mb} MB）：$path"
    else
        fail "磁盘可用空间不足（${free_mb} MB < ${min_mb} MB）：$path"
    fi
}

check_disk_space "$DATA_DIR" "$MIN_FREE_DISK_MB"
check_disk_space "$LOG_DIR"  "$MIN_FREE_DISK_MB"

# -----------------------------------------------------------------------
# 2. 目录权限检查
# -----------------------------------------------------------------------
header "目录权限检查"

check_dir_writable() {
    local dir="$1"
    local label="$2"
    if [ ! -d "$dir" ]; then
        warn "目录不存在，将视为可创建：$dir"
        return
    fi
    if [ -w "$dir" ]; then
        ok "目录可写（$label）：$dir"
    else
        fail "目录不可写（$label）：$dir"
    fi
}

check_dir_writable "$DATA_DIR" "数据目录"
check_dir_writable "$LOG_DIR"  "日志目录"

# -----------------------------------------------------------------------
# 3. 关键文件可读写性检查
# -----------------------------------------------------------------------
header "关键文件可读写性检查"

check_file_readable() {
    local file="$1"
    local label="$2"
    if [ ! -f "$file" ]; then
        warn "文件不存在（首次运行前正常）：$file"
        return
    fi
    if [ -r "$file" ]; then
        ok "文件可读（$label）：$file"
    else
        fail "文件不可读（$label）：$file"
    fi
}

check_file_readable "$CHECKPOINT_FILE"  "检查点文件"
check_file_readable "$APPSETTINGS_FILE" "配置文件 appsettings.json"
check_file_readable "$NLOG_CONFIG_FILE" "日志配置 nlog.config"

# 目标快照文件（按表分片）
TARGET_FILES=$(ls $TARGET_STORE_PATTERN 2>/dev/null || true)
if [ -z "$TARGET_FILES" ]; then
    warn "未发现目标快照文件（首次运行前正常）：$TARGET_STORE_PATTERN"
else
    for f in $TARGET_FILES; do
        check_file_readable "$f" "目标快照"
    done
fi

# -----------------------------------------------------------------------
# 4. 配置文件基本有效性检查
# -----------------------------------------------------------------------
header "配置文件有效性检查"

if command -v python3 &>/dev/null; then
    if python3 -c "
import json, sys

with open('$APPSETTINGS_FILE', 'r', encoding='utf-8') as f:
    content = f.read()

# 剥离行注释（// ...），正确追踪转义字符避免误判字符串边界。
def strip_comments(text):
    result = []
    in_string = False
    i = 0
    while i < len(text):
        c = text[i]
        if in_string:
            result.append(c)
            if c == '\\\\':
                # 转义字符：跳过下一个字符，不改变 in_string 状态。
                i += 1
                if i < len(text):
                    result.append(text[i])
            elif c == '\\\"':
                in_string = False
            i += 1
            continue
        if c == '\\\"':
            in_string = True
            result.append(c)
            i += 1
            continue
        if c == '/' and i + 1 < len(text) and text[i + 1] == '/':
            # 行注释：跳过直到换行。
            while i < len(text) and text[i] != '\\n':
                i += 1
            continue
        result.append(c)
        i += 1
    return ''.join(result)

try:
    json.loads(strip_comments(content))
    sys.exit(0)
except Exception as e:
    print(f'JSON 解析失败：{e}', file=sys.stderr)
    sys.exit(1)
" 2>/dev/null; then
        ok "appsettings.json 格式合法"
    else
        fail "appsettings.json 格式非法，请检查 JSON 语法"
    fi
else
    warn "未安装 python3，跳过配置文件 JSON 格式校验"
fi

# -----------------------------------------------------------------------
# 5. 日志文件健康检查
# -----------------------------------------------------------------------
header "日志文件健康检查"

if [ ! -d "$LOG_DIR" ]; then
    warn "日志目录不存在（首次运行前正常）：$LOG_DIR"
else
    # 检查是否有过大日志文件
    while IFS= read -r -d '' log_file; do
        size_mb=$(du -m "$log_file" | awk '{print $1}')
        if [ "$size_mb" -ge "$LOG_FILE_WARN_MB" ]; then
            warn "日志文件过大（${size_mb} MB >= ${LOG_FILE_WARN_MB} MB）：$log_file"
        fi
    done < <(find "$LOG_DIR" -name "*.log" -print0 2>/dev/null)

    # 检查最近是否有活跃日志写入（-mmin 使用分钟数，避免 -newer 依赖临时文件的可移植性问题）。
    RECENT_LOG=$(find "$LOG_DIR" -name "*.log" -mmin "-$((LOG_ACTIVE_HOURS * 60))" 2>/dev/null | head -1 || true)
    if [ -n "$RECENT_LOG" ]; then
        ok "日志目录最近 ${LOG_ACTIVE_HOURS} 小时内有活跃写入"
    else
        warn "日志目录最近 ${LOG_ACTIVE_HOURS} 小时内无新日志写入，进程可能卡死或停止"
    fi
fi

# -----------------------------------------------------------------------
# 6. 进程存活检查
# -----------------------------------------------------------------------
header "进程存活检查"

check_process() {
    local proc_name="$1"
    if pgrep -f "$proc_name" &>/dev/null; then
        ok "进程存活：$proc_name"
    else
        fail "进程未运行：$proc_name"
    fi
}

check_process "EverydayChain.Hub.Host"

# -----------------------------------------------------------------------
# 7. 目标快照归档文件检查
# -----------------------------------------------------------------------
header "目标快照归档文件检查"

ARCHIVE_COUNT=$(find "$DATA_DIR" -name "*.json.gz" 2>/dev/null | wc -l || echo 0)
if [ "$ARCHIVE_COUNT" -gt 0 ]; then
    ok "发现压缩归档文件 ${ARCHIVE_COUNT} 个（正常）"
    TOTAL_ARCHIVE_MB=$(du -sm "$DATA_DIR"/*.json.gz 2>/dev/null | awk '{sum+=$1} END{print sum+0}')
    if [ "$TOTAL_ARCHIVE_MB" -gt 1024 ]; then
        warn "归档文件总大小超过 1 GB（${TOTAL_ARCHIVE_MB} MB），建议检查保留数量配置"
    fi
else
    warn "未发现压缩归档文件（可能尚未触发归档或已清理）"
fi

# -----------------------------------------------------------------------
# 汇总结果
# -----------------------------------------------------------------------
echo
echo "=============================="
echo "体检汇总"
echo "=============================="
echo "  通过：$PASS 项"
echo "  失败：$FAIL 项"
echo "=============================="

if [ "$FAIL" -gt 0 ]; then
    red "体检未通过，存在 $FAIL 项异常，请根据 [FAIL] 提示逐项处理。"
    exit 1
else
    green "体检全部通过。"
    exit 0
fi
