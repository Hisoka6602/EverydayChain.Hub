using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SyncMergeResult 类型。
/// </summary>
public class SyncMergeResult
{
    /// <summary>
    /// 获取或设置 InsertCount。
    /// </summary>
    public int InsertCount { get; set; }

    /// <summary>
    /// 获取或设置 UpdateCount。
    /// </summary>
    public int UpdateCount { get; set; }

    /// <summary>
    /// 获取或设置 SkipCount。
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 获取或设置 LastSuccessCursorLocal。
    /// </summary>
    public DateTime? LastSuccessCursorLocal { get; set; }

    public IReadOnlyDictionary<string, SyncChangeOperationType> ChangedOperations { get; set; } = new Dictionary<string, SyncChangeOperationType>();
}

