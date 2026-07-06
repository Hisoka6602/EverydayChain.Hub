using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public class SyncDeletionDetectRequest
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    public SyncWindow Window { get; set; } = new();

    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CompareSegmentSize { get; set; } = 20000;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CompareMaxParallelism { get; set; } = 4;
}

