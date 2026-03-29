using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 幂等合并结果。
/// </summary>
public class SyncMergeResult
{
    /// <summary>新增行数。</summary>
    public int InsertCount { get; set; }

    /// <summary>更新行数。</summary>
    public int UpdateCount { get; set; }

    /// <summary>跳过行数。</summary>
    public int SkipCount { get; set; }

    /// <summary>最大成功游标本地时间。</summary>
    public DateTime? LastSuccessCursorLocal { get; set; }

    /// <summary>发生变更的业务键与操作类型映射。</summary>
    public IReadOnlyDictionary<string, SyncChangeOperationType> ChangedOperations { get; set; } = new Dictionary<string, SyncChangeOperationType>();
}
