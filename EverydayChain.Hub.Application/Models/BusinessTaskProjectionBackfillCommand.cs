namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskProjectionBackfillCommand
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime StartTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public DateTime EndTimeLocal { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? TableCode { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MaxCount { get; set; } = 1000;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int BatchSize { get; set; } = 200;
}

