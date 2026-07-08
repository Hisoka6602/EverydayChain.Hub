using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义 ScanMatchResult 类型。
/// </summary>
public sealed class ScanMatchResult
{
    /// <summary>
    /// 获取或设置 IsMatched。
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// 获取或设置 Task。
    /// </summary>
    public BusinessTaskEntity? Task { get; set; }

    /// <summary>
    /// 获取或设置 FailureReason。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}

