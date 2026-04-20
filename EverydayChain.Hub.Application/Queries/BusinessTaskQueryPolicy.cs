using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 业务任务查询策略集合。
/// 该类集中封装查询服务复用的口径判定能力，统一码头归属、7 号码头识别与百分比计算规则。
/// </summary>
public sealed class BusinessTaskQueryPolicy
{
    /// <summary>
    /// 无码头占位文本。
    /// </summary>
    private const string EmptyDockCode = "未分配码头";

    /// <summary>
    /// 回流码头阈值。
    /// </summary>
    private const int RecirculationDockThreshold = 7;

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
    /// 按归并码头编码判定是否回流。
    /// 判定规则：码头编码可解析为整数且大于 7。
    /// </summary>
    /// <param name="resolvedDockCode">归并码头编码。</param>
    /// <returns>是否回流。</returns>
    public bool IsRecirculatedByResolvedDockCode(string? resolvedDockCode)
    {
        if (string.IsNullOrWhiteSpace(resolvedDockCode))
        {
            return false;
        }

        var normalizedDockCode = resolvedDockCode.Trim();
        return int.TryParse(normalizedDockCode, out var dockNumber)
            && dockNumber > RecirculationDockThreshold;
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
