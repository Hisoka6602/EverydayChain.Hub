using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncMergeResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int InsertCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int UpdateCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime? LastSuccessCursorLocal { get; set; }

    public IReadOnlyDictionary<string, SyncChangeOperationType> ChangedOperations { get; set; } = new Dictionary<string, SyncChangeOperationType>();
}

