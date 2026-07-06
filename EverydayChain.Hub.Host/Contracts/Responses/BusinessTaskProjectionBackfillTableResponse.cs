namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskProjectionBackfillTableResponse
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string TableCode { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CandidateCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RemoteRowCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ProjectedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int MissingRemoteCount { get; set; }
}

