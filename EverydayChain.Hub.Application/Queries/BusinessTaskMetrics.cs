using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 业务任务统计规则。
/// </summary>
internal sealed class BusinessTaskMetrics
{
    /// <summary>
    /// 无波次占位文本。
    /// </summary>
    private const string EmptyWaveCode = "未分波次";

    /// <summary>
    /// 无码头占位文本。
    /// </summary>
    private const string EmptyDockCode = "未分配码头";

    /// <summary>
    /// 判断任务是否已分拣完成。
    /// </summary>
    /// <param name="task">业务任务实体。</param>
    /// <returns>是否已分拣。</returns>
    public bool IsSortedTask(BusinessTaskEntity task)
    {
        return task.Status == BusinessTaskStatus.Dropped || task.Status == BusinessTaskStatus.FeedbackPending;
    }

    /// <summary>
    /// 归一化波次编码。
    /// </summary>
    /// <param name="waveCode">原始波次编码。</param>
    /// <returns>归一化后文本。</returns>
    public string NormalizeWaveCode(string? waveCode)
    {
        return string.IsNullOrWhiteSpace(waveCode) ? EmptyWaveCode : waveCode.Trim();
    }

    /// <summary>
    /// 解析任务所属码头号。
    /// </summary>
    /// <param name="task">业务任务实体。</param>
    /// <returns>码头号文本。</returns>
    public string ResolveDockCode(BusinessTaskEntity task)
    {
        if (!string.IsNullOrWhiteSpace(task.ActualChuteCode))
        {
            return task.ActualChuteCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(task.TargetChuteCode))
        {
            return task.TargetChuteCode.Trim();
        }

        return EmptyDockCode;
    }

    /// <summary>
    /// 判断是否 7 号码头。
    /// </summary>
    /// <param name="dockCode">码头号。</param>
    /// <returns>是否 7 号码头。</returns>
    public bool IsDockSeven(string dockCode)
    {
        if (string.IsNullOrWhiteSpace(dockCode))
        {
            return false;
        }

        var trimmed = dockCode.Trim();
        if (int.TryParse(trimmed, out var number))
        {
            return number == 7;
        }

        return string.Equals(trimmed, "7", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 计算百分比。
    /// </summary>
    /// <param name="numerator">分子。</param>
    /// <param name="denominator">分母。</param>
    /// <returns>百分比（保留两位小数）。</returns>
    public decimal CalculatePercent(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0M;
        }

        return Math.Round((decimal)numerator * 100M / denominator, 2, MidpointRounding.AwayFromZero);
    }
}
