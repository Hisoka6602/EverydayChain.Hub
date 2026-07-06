namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class OracleBusinessKeyRowReadRequest
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BusinessKeyColumn { get; set; } = string.Empty;

    public IReadOnlyList<string> RequestedColumns { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> BusinessKeys { get; set; } = Array.Empty<string>();
}

