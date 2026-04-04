#!/usr/bin/env bash
# =============================================================================
# EverydayChain.Hub 稳定性演练脚本
# 用途：标准化执行年度/季度稳定性演练动作，并自动产出演练记录文件。
# 用法：bash scripts/stability-drill.sh [--execute] [--table-code <表编码>] [--record-dir <目录>]
#
# 选项：
#   --execute            执行真实动作；默认仅 dry-run 验证流程
#   --table-code <名称>  指定快照恢复演练所用表编码（默认：SortingTaskTrace）
#   --record-dir <目录>  演练记录目录（默认：仓库根目录 drill-records/）
#
# 输出：
#   drill-records/stability-drill-<时间戳>.log
# =============================================================================

set -euo pipefail

readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly BASE_DIR="$(dirname "$SCRIPT_DIR")"

EXECUTE=false
TABLE_CODE="SortingTaskTrace"
RECORD_DIR="$BASE_DIR/drill-records"

print_usage() {
    local output="${1:-stderr}"
    local usage_text
    usage_text=$(cat <<EOF
用法：bash scripts/stability-drill.sh [--execute] [--table-code <表编码>] [--record-dir <目录>] [--help]

选项：
  --execute            执行真实动作；默认仅 dry-run 验证流程
  --table-code <名称>  指定快照恢复演练所用表编码（默认：SortingTaskTrace）
  --record-dir <目录>  演练记录目录（默认：仓库根目录 drill-records/）
  --help               输出帮助说明
EOF
)
    if [ "$output" = "stdout" ]; then
        printf "%s\n" "$usage_text"
    else
        printf "%s\n" "$usage_text" >&2
    fi
}

validate_option_value() {
    local option_name="$1"
    local option_value="${2:-}"
    if [ -z "$option_value" ] || [[ "$option_value" == --* ]]; then
        echo "[ERROR] 参数 ${option_name} 缺少有效值或误传了其他选项。" >&2
        print_usage
        exit 1
    fi
}

while [ $# -gt 0 ]; do
    case "$1" in
        --execute)
            EXECUTE=true
            shift
            ;;
        --table-code)
            validate_option_value "--table-code" "${2:-}"
            TABLE_CODE="$2"
            shift 2
            ;;
        --record-dir)
            validate_option_value "--record-dir" "${2:-}"
            RECORD_DIR="$2"
            shift 2
            ;;
        --help)
            print_usage stdout
            exit 0
            ;;
        *)
            echo "[ERROR] 未知参数：$1" >&2
            print_usage
            exit 1
            ;;
    esac
done

mkdir -p "$RECORD_DIR"
mkdir -p "$BASE_DIR/data"
TS="$(date +%Y%m%d%H%M%S)"
RECORD_FILE="$RECORD_DIR/stability-drill-$TS.log"

MODE_FLAG="--dry-run"
if $EXECUTE; then
    MODE_FLAG=""
fi

{
    echo "=== EverydayChain.Hub 稳定性演练记录 ==="
    echo "时间：$(date '+%Y-%m-%d %H:%M:%S')"
    echo "模式：$($EXECUTE && echo '真实执行' || echo 'DryRun 验证')"
    echo "快照恢复演练表：$TABLE_CODE"
    echo

    echo "1) 一键体检"
    if bash "$SCRIPT_DIR/health-check.sh"; then
        echo "[INFO] 一键体检结果：通过"
    else
        echo "[WARN] 一键体检结果：未通过（已记录，演练继续执行）"
    fi
    echo

    echo "2) 灾难恢复演练：检查点重置"
    bash "$SCRIPT_DIR/disaster-recovery.sh" checkpoint-reset $MODE_FLAG
    echo

    echo "3) 灾难恢复演练：快照备份"
    bash "$SCRIPT_DIR/disaster-recovery.sh" snapshot-backup $MODE_FLAG
    echo

    echo "4) 灾难恢复演练：快照恢复"
    bash "$SCRIPT_DIR/disaster-recovery.sh" snapshot-restore --table-code "$TABLE_CODE" $MODE_FLAG
    echo

    echo "5) 灾难恢复演练：归档清理"
    bash "$SCRIPT_DIR/disaster-recovery.sh" archive-cleanup $MODE_FLAG
    echo

    echo "=== 演练结束 ==="
} | tee "$RECORD_FILE"

echo "[OK] 演练记录已生成：$RECORD_FILE"
