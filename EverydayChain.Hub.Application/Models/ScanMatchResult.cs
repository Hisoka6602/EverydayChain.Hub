using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ScanMatchResult
{
    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public BusinessTaskEntity? Task { get; set; }

    /// <summary>
    /// 获取或设置当前属性值。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}

