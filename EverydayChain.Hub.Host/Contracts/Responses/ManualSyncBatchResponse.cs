namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ManualSyncBatchResponse
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ReadCount { get; set; }

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
    public int DeleteCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string? ErrorMessage { get; set; }
}

