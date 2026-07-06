namespace EverydayChain.Hub.Host.Contracts.Responses;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskProjectionBackfillPreviewResponse
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
    public IReadOnlyList<BusinessTaskProjectionBackfillPreviewTableResponse> Tables { get; set; } = [];
}

