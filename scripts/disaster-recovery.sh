#!/usr/bin/env bash
# =============================================================================
# EverydayChain.Hub 灾难恢复脚本
# 用途：在检查点损坏、目标快照损坏、磁盘写满等场景下，执行数据恢复与服务恢复操作。
# 用法：bash scripts/disaster-recovery.sh <操作> [选项]
#
# 支持的操作：
#   checkpoint-reset   清空检查点文件，服务重启后将从配置的 StartTimeLocal 重新同步
#   snapshot-restore   从最近的压缩归档（.json.gz）恢复指定表的目标快照文件
#   snapshot-backup    备份当前全部快照文件到指定目录
#   archive-cleanup    清理超出保留数量的最旧压缩归档文件
#   full-reset         清空检查点与全部快照（完全重置，需 --confirm 参数确认）
#
# 选项：
#   --data-dir <路径>   数据目录路径（默认：脚本同级目录下的 data/）
#   --backup-dir <路径> 备份目标目录（默认：data/backup/）
#   --table-code <名称> 指定表编码（用于 snapshot-restore 操作）
#   --dry-run           仅输出将要执行的操作，不实际修改任何文件
#   --confirm           确认执行高危操作（full-reset 必填）
#
# 退出码：
#   0   操作成功或 dry-run 输出完成
#   1   参数错误或操作失败
# =============================================================================

set -euo pipefail

# -----------------------------------------------------------------------
# 常量定义
# -----------------------------------------------------------------------
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly BASE_DIR="$(dirname "$SCRIPT_DIR")"
readonly CHECKPOINT_FILENAME="sync-checkpoints.json"
readonly TARGET_STORE_PREFIX="sync-target-store"

# -----------------------------------------------------------------------
# 默认参数
# -----------------------------------------------------------------------
OPERATION=""
DATA_DIR="$BASE_DIR/data"
BACKUP_DIR="$DATA_DIR/backup"
TABLE_CODE=""
DRY_RUN=false
CONFIRM=false

# -----------------------------------------------------------------------
# 工具函数
# -----------------------------------------------------------------------
green()  { echo -e "\033[32m$*\033[0m"; }
red()    { echo -e "\033[31m$*\033[0m"; }
yellow() { echo -e "\033[33m$*\033[0m"; }
info()   { echo "[INFO]  $*"; }
ok()     { green "[OK]    $*"; }
warn()   { yellow "[WARN]  $*"; }
error()  { red   "[ERROR] $*" >&2; }

# 从归档文件名中提取表编码。
# 文件名格式：sync-target-store.<TableCode>.<timestamp>.json.gz（至少 4 个点分隔段）。
# 验证段数量后提取第 2 段（index 1）作为 TableCode；格式非法时返回空字符串。
extract_table_code_from_archive() {
    local filename="$1"
    local seg_count; seg_count=$(echo "$filename" | awk -F'.' '{print NF}')
    if [ "$seg_count" -lt 4 ]; then
        echo ""
        return 1
    fi
    echo "$filename" | awk -F'.' '{print $2}'
}

# dry-run 安全执行函数：dry-run 时仅打印命令，不实际执行。
safe_exec() {
    if $DRY_RUN; then
        yellow "[DRY-RUN] $*"
    else
        eval "$@"
    fi
}

# -----------------------------------------------------------------------
# 参数解析
# -----------------------------------------------------------------------
if [ $# -lt 1 ]; then
    error "缺少操作参数。用法：bash scripts/disaster-recovery.sh <操作> [选项]"
    error "支持操作：checkpoint-reset | snapshot-restore | snapshot-backup | archive-cleanup | full-reset"
    exit 1
fi

OPERATION="$1"
shift

while [ $# -gt 0 ]; do
    case "$1" in
        --data-dir)    DATA_DIR="$2";    shift 2 ;;
        --backup-dir)  BACKUP_DIR="$2";  shift 2 ;;
        --table-code)  TABLE_CODE="$2";  shift 2 ;;
        --dry-run)     DRY_RUN=true;     shift ;;
        --confirm)     CONFIRM=true;     shift ;;
        *)
            error "未知选项：$1"
            exit 1
            ;;
    esac
done

readonly CHECKPOINT_FILE="$DATA_DIR/$CHECKPOINT_FILENAME"

# -----------------------------------------------------------------------
# 前置检查：数据目录必须存在
# -----------------------------------------------------------------------
if [ ! -d "$DATA_DIR" ]; then
    error "数据目录不存在：$DATA_DIR"
    exit 1
fi

# -----------------------------------------------------------------------
# 操作：checkpoint-reset — 清空检查点文件
# 说明：服务重启后将从各表配置的 StartTimeLocal 重新全量同步。
#       清空前自动备份原文件，备份路径输出到日志。
# -----------------------------------------------------------------------
do_checkpoint_reset() {
    info "操作：checkpoint-reset（清空检查点文件）"
    info "数据目录：$DATA_DIR"

    if [ ! -f "$CHECKPOINT_FILE" ]; then
        warn "检查点文件不存在，无需清空：$CHECKPOINT_FILE"
        return 0
    fi

    # 先备份
    local ts; ts=$(date +%Y%m%d%H%M%S)
    local backup_file="$CHECKPOINT_FILE.bak.$ts"
    safe_exec "cp '$CHECKPOINT_FILE' '$backup_file'"
    ok "已备份检查点文件：$backup_file"

    # 清空（写入空 JSON 对象）
    safe_exec "echo '{}' > '$CHECKPOINT_FILE'"
    ok "已清空检查点文件：$CHECKPOINT_FILE"
    warn "请重启服务，服务将从各表 StartTimeLocal 重新全量同步。"
}

# -----------------------------------------------------------------------
# 操作：snapshot-restore — 从压缩归档恢复目标快照
# 说明：找到指定表（或全部表）最近的 .json.gz 归档，解压覆盖当前 .json 快照。
#       恢复前自动备份当前快照。
# -----------------------------------------------------------------------
do_snapshot_restore() {
    info "操作：snapshot-restore（从压缩归档恢复目标快照）"
    info "数据目录：$DATA_DIR"
    [ -n "$TABLE_CODE" ] && info "指定表编码：$TABLE_CODE"

    # 确定待恢复的表编码列表
    local pattern
    if [ -n "$TABLE_CODE" ]; then
        pattern="$DATA_DIR/${TARGET_STORE_PREFIX}.${TABLE_CODE}.*.json.gz"
    else
        pattern="$DATA_DIR/${TARGET_STORE_PREFIX}.*.*.json.gz"
    fi

    shopt -s nullglob
    local archives=($pattern)
    shopt -u nullglob

    if [ ${#archives[@]} -eq 0 ]; then
        warn "未找到匹配的压缩归档文件：$pattern"
        return 0
    fi

    # 按表分组，取每个表最新的归档
    declare -A latest_per_table
    for archive in "${archives[@]}"; do
        local filename; filename="$(basename "$archive")"
        # 使用验证函数提取 TableCode，格式非法时跳过该文件。
        local table_code; table_code=$(extract_table_code_from_archive "$filename") || {
            warn "  文件名格式非法，跳过：$archive"
            continue
        }
        if [ -z "$table_code" ]; then
            warn "  无法从文件名提取 TableCode，跳过：$archive"
            continue
        fi
        if [ -z "${latest_per_table[$table_code]+_}" ] || \
           [[ "$archive" > "${latest_per_table[$table_code]}" ]]; then
            latest_per_table[$table_code]="$archive"
        fi
    done

    for table_code in "${!latest_per_table[@]}"; do
        local archive="${latest_per_table[$table_code]}"
        local target_file="$DATA_DIR/${TARGET_STORE_PREFIX}.${table_code}.json"

        info "  表：$table_code"
        info "    归档文件：$archive"
        info "    目标快照：$target_file"

        # 备份当前快照（若存在）
        if [ -f "$target_file" ]; then
            local ts; ts=$(date +%Y%m%d%H%M%S)
            local backup_file="${target_file}.before-restore.$ts"
            safe_exec "cp '$target_file' '$backup_file'"
            ok "    已备份当前快照：$backup_file"
        fi

        # 解压归档到目标快照路径；解压失败时清理临时文件并报错。
        if $DRY_RUN; then
            yellow "[DRY-RUN] gunzip -c '$archive' > '${target_file}.tmp' && mv '${target_file}.tmp' '$target_file'"
        else
            if gunzip -c "$archive" > "${target_file}.tmp"; then
                mv "${target_file}.tmp" "$target_file"
            else
                rm -f "${target_file}.tmp"
                error "    解压归档失败，已清理临时文件：${target_file}.tmp"
                continue
            fi
        fi
        ok "    已从归档恢复快照：$target_file"
    done

    warn "快照恢复完成。建议同步执行 checkpoint-reset，避免窗口错位。"
}

# -----------------------------------------------------------------------
# 操作：snapshot-backup — 备份当前全部快照文件
# 说明：将全部 .json 快照文件拷贝到备份目录，以时间戳命名子目录。
# -----------------------------------------------------------------------
do_snapshot_backup() {
    info "操作：snapshot-backup（备份当前全部快照文件）"
    local ts; ts=$(date +%Y%m%d%H%M%S)
    local dest_dir="$BACKUP_DIR/$ts"

    shopt -s nullglob
    local snapshots=("$DATA_DIR"/${TARGET_STORE_PREFIX}.*.json)
    local checkpoint_exists=false
    [ -f "$CHECKPOINT_FILE" ] && checkpoint_exists=true
    shopt -u nullglob

    if [ ${#snapshots[@]} -eq 0 ] && ! $checkpoint_exists; then
        warn "未发现任何快照或检查点文件，无需备份。"
        return 0
    fi

    safe_exec "mkdir -p '$dest_dir'"

    for snap in "${snapshots[@]}"; do
        safe_exec "cp '$snap' '$dest_dir/'"
        ok "  已备份：$(basename "$snap") -> $dest_dir/"
    done

    if $checkpoint_exists; then
        safe_exec "cp '$CHECKPOINT_FILE' '$dest_dir/'"
        ok "  已备份：$CHECKPOINT_FILENAME -> $dest_dir/"
    fi

    ok "备份完成：$dest_dir"
}

# -----------------------------------------------------------------------
# 操作：archive-cleanup — 清理超出保留数量的最旧归档
# 说明：按各表分组，保留最新 N 个 .json.gz 归档，默认 N=7。
# -----------------------------------------------------------------------
do_archive_cleanup() {
    local max_count="${MAX_ARCHIVE_COUNT:-7}"
    info "操作：archive-cleanup（清理最旧压缩归档，每表保留最多 $max_count 个）"

    shopt -s nullglob
    local all_archives=("$DATA_DIR"/${TARGET_STORE_PREFIX}.*.*.json.gz)
    shopt -u nullglob

    if [ ${#all_archives[@]} -eq 0 ]; then
        warn "未发现任何压缩归档文件，跳过清理。"
        return 0
    fi

    # 按表编码分组
    declare -A table_archives
    for archive in "${all_archives[@]}"; do
        local filename; filename="$(basename "$archive")"
        # 使用验证函数提取 TableCode，格式非法时跳过该文件。
        local table_code; table_code=$(extract_table_code_from_archive "$filename") || {
            warn "  文件名格式非法，跳过：$archive"
            continue
        }
        if [ -z "$table_code" ]; then
            warn "  无法从文件名提取 TableCode，跳过：$archive"
            continue
        fi
        table_archives[$table_code]+="$archive"$'\n'
    done

    for table_code in "${!table_archives[@]}"; do
        local sorted_archives
        IFS=$'\n' read -r -d '' -a sorted_archives < <(
            echo -n "${table_archives[$table_code]}" | sort
        ) || true

        local count=${#sorted_archives[@]}
        local excess=$((count - max_count))
        if [ $excess -le 0 ]; then
            info "  表 $table_code：共 $count 个归档，无需清理。"
            continue
        fi

        info "  表 $table_code：共 $count 个归档，清理最旧 $excess 个。"
        for (( i=0; i<excess; i++ )); do
            safe_exec "rm -f '${sorted_archives[$i]}'"
            ok "    已删除：${sorted_archives[$i]}"
        done
    done
}

# -----------------------------------------------------------------------
# 操作：full-reset — 完全重置（清空检查点与全部快照）
# 说明：危险操作，需 --confirm 参数确认。执行前自动备份全部数据。
# -----------------------------------------------------------------------
do_full_reset() {
    if ! $CONFIRM; then
        error "full-reset 为高危操作，需添加 --confirm 参数确认执行。"
        error "执行将清空全部检查点与快照文件，服务重启后将从 StartTimeLocal 重新全量同步。"
        exit 1
    fi

    info "操作：full-reset（完全重置）"
    warn "该操作将清空全部检查点与快照文件！"

    # 先执行备份
    do_snapshot_backup

    # 清空检查点
    if [ -f "$CHECKPOINT_FILE" ]; then
        safe_exec "echo '{}' > '$CHECKPOINT_FILE'"
        ok "已清空检查点文件：$CHECKPOINT_FILE"
    fi

    # 清空全部快照
    shopt -s nullglob
    local snapshots=("$DATA_DIR"/${TARGET_STORE_PREFIX}.*.json)
    shopt -u nullglob

    for snap in "${snapshots[@]}"; do
        safe_exec "rm -f '$snap'"
        ok "已删除快照：$snap"
    done

    warn "完全重置完成。请重启服务，服务将从各表 StartTimeLocal 重新全量同步。"
}

# -----------------------------------------------------------------------
# 分发操作
# -----------------------------------------------------------------------
echo
echo "=============================="
echo "EverydayChain.Hub 灾难恢复工具"
echo "=============================="
$DRY_RUN && yellow "[DRY-RUN 模式：仅输出将要执行的操作，不实际修改文件]"
echo

case "$OPERATION" in
    checkpoint-reset)  do_checkpoint_reset ;;
    snapshot-restore)  do_snapshot_restore ;;
    snapshot-backup)   do_snapshot_backup ;;
    archive-cleanup)   do_archive_cleanup ;;
    full-reset)        do_full_reset ;;
    *)
        error "未知操作：$OPERATION"
        error "支持操作：checkpoint-reset | snapshot-restore | snapshot-backup | archive-cleanup | full-reset"
        exit 1
        ;;
esac

echo
ok "操作完成：$OPERATION"
