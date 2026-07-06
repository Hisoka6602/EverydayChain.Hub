namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskProjectionBackfillResponse
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int ProcessedTableCount { get; set; }

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

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<BusinessTaskProjectionBackfillTableResponse> Tables { get; set; } = [];
}

