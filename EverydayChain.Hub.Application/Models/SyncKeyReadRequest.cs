using EverydayChain.Hub.Domain.Sync;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 SyncKeyReadRequest 类型。
/// </summary>
public class SyncKeyReadRequest
{
    /// <summary>
    /// 获取或设置 TableCode。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 CursorColumn。
    /// </summary>
    public string CursorColumn { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceSchema。
    /// </summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceTable。
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    public SyncWindow Window { get; set; } = new();

    public IReadOnlyList<string> UniqueKeys { get; set; } = Array.Empty<string>();
}

