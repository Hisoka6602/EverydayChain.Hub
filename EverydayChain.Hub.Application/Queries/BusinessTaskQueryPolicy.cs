using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 定义 BusinessTaskQueryPolicy 类型。
/// </summary>
internal sealed class BusinessTaskQueryPolicy
{
    /// <summary>
    /// 存储 EmptyDockCode 字段。
    /// </summary>
    private const string EmptyDockCode = "未分配码头";

    /// <summary>
    /// 存储 RecirculationDockThreshold 字段。
    /// </summary>
    private const int RecirculationDockThreshold = 7;

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

    public decimal CalculatePercent(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0M;
        }

        return Math.Round((decimal)numerator * 100M / denominator, 3, MidpointRounding.AwayFromZero);
    }
}

