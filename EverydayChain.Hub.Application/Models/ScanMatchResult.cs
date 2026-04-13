using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Models;

/// <summary>
/// 扫描匹配结果模型。
/// </summary>
public sealed class ScanMatchResult
{
    /// <summary>
    /// 是否成功匹配到业务任务。
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    /// 匹配到的业务任务实体；未匹配时为 <c>null</c>。
    /// </summary>
    public BusinessTaskEntity? Task { get; set; }

    /// <summary>
    /// 未匹配的失败原因；匹配成功时为空。
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}
