using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using System.Text;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 分拣报表查询服务实现。
/// </summary>
public sealed class SortingReportQueryService : ISortingReportQueryService
{
    /// <summary>
    /// 业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 业务任务统计规则。
    /// </summary>
    private readonly BusinessTaskQueryPolicy _queryPolicy = new();

    /// <summary>
    /// 初始化分拣报表查询服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    public SortingReportQueryService(IBusinessTaskRepository businessTaskRepository)
    {
        _businessTaskRepository = businessTaskRepository;
    }

    /// <inheritdoc/>
    public async Task<SortingReportQueryResult> QueryAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤 1：校验时间区间。
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new SortingReportQueryResult
            {
                StartTimeLocal = request.StartTimeLocal,
                EndTimeLocal = request.EndTimeLocal
            };
        }

        // 步骤 2：在仓储侧执行码头维度筛选与聚合。
        var selectedDockCode = string.IsNullOrWhiteSpace(request.DockCode) ? null : request.DockCode.Trim();
        var dockRows = await _businessTaskRepository.AggregateDockDashboardAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
            null,
            selectedDockCode,
            cancellationToken);
        var rows = dockRows
            .Select(row => new SortingReportRow
            {
                DockCode = row.DockCode,
                SplitTotalCount = row.SplitTotalCount,
                FullCaseTotalCount = row.FullCaseTotalCount,
                SplitSortedCount = row.SplitSortedCount,
                FullCaseSortedCount = row.FullCaseSortedCount,
                RecirculatedCount = row.RecirculatedCount,
                ExceptionCount = _queryPolicy.IsDockSeven(row.DockCode) ? row.ExceptionCount : 0
            })
            .OrderBy(row => row.DockCode, StringComparer.Ordinal)
            .ToList();

        // 步骤 3：返回报表结果。
        return new SortingReportQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            SelectedDockCode = selectedDockCode,
            Rows = rows
        };
    }

    /// <inheritdoc/>
    public async Task<string> ExportCsvAsync(SortingReportQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤 1：复用查询结果，确保导出与查询口径一致。
        var result = await QueryAsync(request, cancellationToken);

        // 步骤 2：生成 CSV 文本。
        var builder = new StringBuilder();
        builder.AppendLine("码头号,拆零总数,整件总数,拆零分拣数,整件分拣数,回流数,异常数");
        foreach (var row in result.Rows)
        {
            builder.AppendLine($"{EscapeCsvField(row.DockCode)},{row.SplitTotalCount},{row.FullCaseTotalCount},{row.SplitSortedCount},{row.FullCaseSortedCount},{row.RecirculatedCount},{row.ExceptionCount}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// CSV 字段转义。
    /// </summary>
    /// <param name="value">原始字段值。</param>
    /// <returns>转义后字段。</returns>
    private static string EscapeCsvField(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
