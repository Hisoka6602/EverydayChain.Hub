namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class BusinessTaskSeedResult
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
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int RequestedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int FilteredEmptyCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int DeduplicatedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int CandidateCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int InsertedCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public int SkippedExistingCount { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<string> InsertedBarcodes { get; set; } = [];

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public IReadOnlyList<string> SkippedBarcodes { get; set; } = [];

    public static BusinessTaskSeedResult Fail(string message)
    {
        return new BusinessTaskSeedResult
        {
            IsSuccess = false,
            Message = message,
            InsertedBarcodes = [],
            SkippedBarcodes = []
        };
    }
}

