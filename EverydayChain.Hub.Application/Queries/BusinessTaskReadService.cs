using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 业务任务查询服务实现。
/// </summary>
public sealed class BusinessTaskReadService : IBusinessTaskReadService
{
    /// <summary>
    /// 业务任务仓储。
    /// </summary>
    private readonly IBusinessTaskRepository _businessTaskRepository;

    /// <summary>
    /// 业务任务统计规则。
    /// </summary>
    private readonly BusinessTaskMetrics _metrics = new();

    /// <summary>
    /// 初始化业务任务查询服务。
    /// </summary>
    /// <param name="businessTaskRepository">业务任务仓储。</param>
    public BusinessTaskReadService(IBusinessTaskRepository businessTaskRepository)
    {
        _businessTaskRepository = businessTaskRepository;
    }

    /// <inheritdoc/>
    public Task<BusinessTaskQueryResult> QueryTasksAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        return QueryCoreAsync(request, task => true, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<BusinessTaskQueryResult> QueryExceptionsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        return QueryCoreAsync(request, task => task.IsException || task.Status == BusinessTaskStatus.Exception, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<BusinessTaskQueryResult> QueryRecirculationsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        return QueryCoreAsync(request, task => task.IsRecirculated, cancellationToken);
    }

    /// <summary>
    /// 统一查询主流程。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="extraPredicate">附加过滤条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>分页结果。</returns>
    private async Task<BusinessTaskQueryResult> QueryCoreAsync(
        BusinessTaskQueryRequest request,
        Func<BusinessTaskEntity, bool> extraPredicate,
        CancellationToken cancellationToken)
    {
        // 步骤 1：校验时间区间。
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return new BusinessTaskQueryResult
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        // 步骤 2：读取基础数据并执行多条件筛选。
        var tasks = await _businessTaskRepository.FindByCreatedTimeRangeAsync(request.StartTimeLocal, request.EndTimeLocal, cancellationToken);
        var normalizedWaveCode = NormalizeOptionalValue(request.WaveCode);
        var normalizedBarcode = NormalizeOptionalValue(request.Barcode);
        var normalizedDockCode = NormalizeOptionalValue(request.DockCode);
        var normalizedChuteCode = NormalizeOptionalValue(request.ChuteCode);

        var filteredTasks = tasks
            .Where(task => MatchOptionalValue(_metrics.NormalizeWaveCode(task.WaveCode), normalizedWaveCode))
            .Where(task => MatchOptionalValue(task.Barcode, normalizedBarcode))
            .Where(task => MatchOptionalValue(_metrics.ResolveDockCode(task), normalizedDockCode))
            .Where(task => MatchOptionalValue(task.TargetChuteCode, normalizedChuteCode) || MatchOptionalValue(task.ActualChuteCode, normalizedChuteCode))
            .Where(extraPredicate)
            .OrderByDescending(task => task.CreatedTimeLocal)
            .ToList();

        // 步骤 3：分页并映射输出。
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 50 : request.PageSize;
        var skip = (pageNumber - 1) * pageSize;
        var pagedTasks = filteredTasks
            .Skip(skip)
            .Take(pageSize)
            .Select(MapItem)
            .ToList();

        return new BusinessTaskQueryResult
        {
            TotalCount = filteredTasks.Count,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = pagedTasks
        };
    }

    /// <summary>
    /// 归一化可选文本。
    /// </summary>
    /// <param name="value">原始值。</param>
    /// <returns>归一化值。</returns>
    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 匹配可选值。
    /// </summary>
    /// <param name="actual">实际值。</param>
    /// <param name="expected">期望值。</param>
    /// <returns>是否匹配。</returns>
    private static bool MatchOptionalValue(string? actual, string? expected)
    {
        if (expected is null)
        {
            return true;
        }

        return string.Equals(actual?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 映射查询结果项。
    /// </summary>
    /// <param name="task">业务任务实体。</param>
    /// <returns>查询结果项。</returns>
    private BusinessTaskQueryItem MapItem(BusinessTaskEntity task)
    {
        return new BusinessTaskQueryItem
        {
            TaskCode = task.TaskCode,
            Barcode = task.Barcode,
            WaveCode = task.WaveCode,
            SourceType = task.SourceType,
            Status = task.Status,
            TargetChuteCode = task.TargetChuteCode,
            ActualChuteCode = task.ActualChuteCode,
            DockCode = _metrics.ResolveDockCode(task),
            IsRecirculated = task.IsRecirculated,
            IsException = task.IsException || task.Status == BusinessTaskStatus.Exception,
            CreatedTimeLocal = task.CreatedTimeLocal
        };
    }
}
