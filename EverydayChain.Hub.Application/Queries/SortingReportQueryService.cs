using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;
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

        // 步骤 2：拉取任务并执行码头维度筛选。
        var selectedDockCode = string.IsNullOrWhiteSpace(request.DockCode) ? null : request.DockCode.Trim();
        var tasks = await _businessTaskRepository.FindByCreatedTimeRangeAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);
        var filteredTasks = string.IsNullOrWhiteSpace(selectedDockCode)
            ? tasks
            : tasks.Where(task => string.Equals(_queryPolicy.ResolveDockCode(task), selectedDockCode, StringComparison.OrdinalIgnoreCase)).ToList();

        // 步骤 3：按码头聚合统计。
        var counters = new Dictionary<string, ReportCounter>(StringComparer.OrdinalIgnoreCase);
        foreach (var task in filteredTasks)
        {
            var dockCode = _queryPolicy.ResolveDockCode(task);
            if (!counters.TryGetValue(dockCode, out var counter))
            {
                counter = new ReportCounter();
                counters.Add(dockCode, counter);
            }

            var isSorted = _queryPolicy.IsSortedTask(task);
            if (task.SourceType == BusinessTaskSourceType.Split)
            {
                counter.SplitTotalCount++;
                if (isSorted)
                {
                    counter.SplitSortedCount++;
                }
            }
            else if (task.SourceType == BusinessTaskSourceType.FullCase)
            {
                counter.FullCaseTotalCount++;
                if (isSorted)
                {
                    counter.FullCaseSortedCount++;
                }
            }

            if (task.IsRecirculated)
            {
                counter.RecirculatedCount++;
            }

            if (_queryPolicy.IsDockSeven(dockCode) && (task.IsException || task.Status == BusinessTaskStatus.Exception))
            {
                counter.ExceptionCount++;
            }
        }

        var rows = counters
            .Select(pair => new SortingReportRow
            {
                DockCode = pair.Key,
                SplitTotalCount = pair.Value.SplitTotalCount,
                FullCaseTotalCount = pair.Value.FullCaseTotalCount,
                SplitSortedCount = pair.Value.SplitSortedCount,
                FullCaseSortedCount = pair.Value.FullCaseSortedCount,
                RecirculatedCount = pair.Value.RecirculatedCount,
                ExceptionCount = pair.Value.ExceptionCount
            })
            .OrderBy(row => row.DockCode, StringComparer.Ordinal)
            .ToList();

        // 步骤 4：返回报表结果。
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

    /// <summary>
    /// 报表聚合计数器。
    /// </summary>
    private sealed class ReportCounter
    {
        /// <summary>
        /// 拆零总数。
        /// </summary>
        public int SplitTotalCount { get; set; }

        /// <summary>
        /// 整件总数。
        /// </summary>
        public int FullCaseTotalCount { get; set; }

        /// <summary>
        /// 拆零分拣数。
        /// </summary>
        public int SplitSortedCount { get; set; }

        /// <summary>
        /// 整件分拣数。
        /// </summary>
        public int FullCaseSortedCount { get; set; }

        /// <summary>
        /// 回流数。
        /// </summary>
        public int RecirculatedCount { get; set; }

        /// <summary>
        /// 异常数。
        /// </summary>
        public int ExceptionCount { get; set; }
    }
}
