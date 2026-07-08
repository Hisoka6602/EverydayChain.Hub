namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 BusinessTaskSeedResult 类型。
/// </summary>
public sealed class BusinessTaskSeedResult
{
    /// <summary>
    /// 获取或设置 IsSuccess。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 获取或设置 Message。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 TargetTableName。
    /// </summary>
    public string TargetTableName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 RequestedCount。
    /// </summary>
    public int RequestedCount { get; set; }

    /// <summary>
    /// 获取或设置 FilteredEmptyCount。
    /// </summary>
    public int FilteredEmptyCount { get; set; }

    /// <summary>
    /// 获取或设置 DeduplicatedCount。
    /// </summary>
    public int DeduplicatedCount { get; set; }

    /// <summary>
    /// 获取或设置 CandidateCount。
    /// </summary>
    public int CandidateCount { get; set; }

    /// <summary>
    /// 获取或设置 InsertedCount。
    /// </summary>
    public int InsertedCount { get; set; }

    /// <summary>
    /// 获取或设置 SkippedExistingCount。
    /// </summary>
    public int SkippedExistingCount { get; set; }

    /// <summary>
    /// 获取或设置 InsertedBarcodes。
    /// </summary>
    public IReadOnlyList<string> InsertedBarcodes { get; set; } = [];

    /// <summary>
    /// 获取或设置 SkippedBarcodes。
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

