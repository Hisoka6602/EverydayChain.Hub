namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskProjectionBackfillPreviewResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string Message { get; set; } = string.Empty;

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
    public IReadOnlyList<BusinessTaskProjectionBackfillPreviewTableResult> Tables { get; set; } = [];

    public static BusinessTaskProjectionBackfillPreviewResult Fail(string message)
    {
        return new BusinessTaskProjectionBackfillPreviewResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}

