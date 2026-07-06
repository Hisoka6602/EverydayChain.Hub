namespace EverydayChain.Hub.Host.Contracts.Requests;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskProjectionBackfillRequest
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

