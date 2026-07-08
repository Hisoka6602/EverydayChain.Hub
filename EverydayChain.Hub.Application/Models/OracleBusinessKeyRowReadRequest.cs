namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 OracleBusinessKeyRowReadRequest 类型。
/// </summary>
public sealed class OracleBusinessKeyRowReadRequest
{
    /// <summary>
    /// 获取或设置 TableCode。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceSchema。
    /// </summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 SourceTable。
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 BusinessKeyColumn。
    /// </summary>
    public string BusinessKeyColumn { get; set; } = string.Empty;

    public IReadOnlyList<string> RequestedColumns { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> BusinessKeys { get; set; } = Array.Empty<string>();
}

