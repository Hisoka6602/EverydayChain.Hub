using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Queries;

/// <summary>
/// 提供箱子追踪查询能力。
/// 该服务只做查询口径整合，不改变分拣机既有扫描上传、格口解析和落格回传的处理协议。
/// </summary>
public sealed class BoxTrackingQueryService(
    IScanLogRepository scanLogRepository,
    IBusinessTaskRepository businessTaskRepository) : IBoxTrackingQueryService
{
    /// <summary>
    /// 存储业务任务查询策略。
    /// </summary>
    private readonly BusinessTaskQueryPolicy _queryPolicy = new();

    /// <summary>
    /// 查询箱子追踪分页结果。
    /// </summary>
    /// <param name="request">查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>箱子追踪分页结果。</returns>
    public async Task<BoxTrackingQueryResult> QueryAsync(BoxTrackingQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤：先规范分页参数，再根据筛选类型选择数据库分页或全量筛选路径。
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 50 : request.PageSize;
        var result = new BoxTrackingQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return result;
        }

        if (!HasTaskLevelFilters(request))
        {
            var skip = (pageNumber - 1) * pageSize;
            var page = await scanLogRepository.QueryPageAsync(
                request.StartTimeLocal,
                request.EndTimeLocal,
                request.BoxId,
                request.Scanner,
                skip,
                pageSize,
                cancellationToken);
            var taskIds = page.Items
                .Where(item => item.BusinessTaskId.HasValue)
                .Select(item => item.BusinessTaskId!.Value)
                .Distinct()
                .ToArray();
            var taskMap = await businessTaskRepository.GetByIdsAsync(taskIds, cancellationToken);
            result.TotalCount = page.TotalCount;
            result.Items = page.Items
                .Select(log => MapItem(log, ResolveTask(log, taskMap)))
                .Where(item => item is not null)
                .Select(item => item!)
                .ToList();
            return result;
        }

        var items = await QueryAllAsync(request, cancellationToken);
        var pageSkip = (pageNumber - 1) * pageSize;
        result.TotalCount = items.Count;
        result.Items = items.Skip(pageSkip).Take(pageSize).ToList();
        return result;
    }

    /// <summary>
    /// 查询满足条件的全部箱子追踪结果。
    /// </summary>
    /// <param name="request">查询条件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>全部箱子追踪结果。</returns>
    public async Task<IReadOnlyList<BoxTrackingItem>> QueryAllAsync(BoxTrackingQueryRequest request, CancellationToken cancellationToken)
    {
        // 步骤：先按扫描日志范围取数，再关联业务任务并执行任务级筛选。
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return Array.Empty<BoxTrackingItem>();
        }

        var logs = await scanLogRepository.QueryRangeAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
            request.BoxId,
            request.Scanner,
            cancellationToken);

        var taskIds = logs
            .Where(item => item.BusinessTaskId.HasValue)
            .Select(item => item.BusinessTaskId!.Value)
            .Distinct()
            .ToArray();
        var taskMap = await businessTaskRepository.GetByIdsAsync(taskIds, cancellationToken);
        var normalizedOrderId = NormalizeOptionalText(request.OrderId);
        var normalizedStoreId = NormalizeOptionalText(request.StoreId);
        var normalizedChuteCode = NormalizeOptionalText(request.ChuteCode);

        return logs
            .Select(log => MapItem(log, ResolveTask(log, taskMap)))
            .Where(item => item is not null)
            .Select(item => item!)
            .Where(item => normalizedOrderId is null || string.Equals(item.OrderId, normalizedOrderId, StringComparison.OrdinalIgnoreCase))
            .Where(item => normalizedStoreId is null || string.Equals(item.StoreId, normalizedStoreId, StringComparison.OrdinalIgnoreCase))
            .Where(item => normalizedChuteCode is null || string.Equals(item.Chute, normalizedChuteCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// 将扫描日志和业务任务映射为箱子追踪项。
    /// </summary>
    /// <param name="log">扫描日志。</param>
    /// <param name="task">业务任务。</param>
    /// <returns>箱子追踪项。</returns>
    private BoxTrackingItem MapItem(ScanLogEntity log, BusinessTaskEntity? task)
    {
        // 步骤：boxId 继续直接映射扫描日志 Barcode，只澄清命名，不修改既有字段语义。
        var chute = task is null ? null : ResolveChute(task);
        return new BoxTrackingItem
        {
            BoxId = log.Barcode,
            TaskCode = task?.TaskCode ?? log.TaskCode,
            WaveCode = task?.WaveCode,
            OrderId = task?.OrderId,
            StoreId = task?.StoreId,
            StoreName = task?.StoreName,
            ProductCode = task?.ProductCode,
            PickLocation = task?.PickLocation,
            Scanner = log.DeviceCode,
            ScannedAtLocal = log.ScanTimeLocal,
            Chute = chute,
            Status = ResolveStatus(log, task),
            IsMatched = log.IsMatched,
            FailureReason = log.FailureReason
        };
    }

    /// <summary>
    /// 判断请求是否包含任务级筛选条件。
    /// </summary>
    /// <param name="request">查询条件。</param>
    /// <returns>包含任务级筛选时返回真。</returns>
    private static bool HasTaskLevelFilters(BoxTrackingQueryRequest request)
    {
        // 步骤：仅订单、门店、码头依赖业务任务字段，存在时必须走全量筛选路径。
        return !string.IsNullOrWhiteSpace(request.OrderId)
            || !string.IsNullOrWhiteSpace(request.StoreId)
            || !string.IsNullOrWhiteSpace(request.ChuteCode);
    }

    /// <summary>
    /// 执行 ResolveTask 方法。
    /// </summary>
    private static BusinessTaskEntity? ResolveTask(
        ScanLogEntity log,
        IReadOnlyDictionary<long, BusinessTaskEntity> taskMap)
    {
        // 步骤：执行 ResolveTask 方法的核心处理流程。
        if (!log.BusinessTaskId.HasValue)
        {
            return null;
        }

        return taskMap.TryGetValue(log.BusinessTaskId.Value, out var task) ? task : null;
    }

    private static string? ResolveChute(BusinessTaskEntity task)
    {
        if (!string.IsNullOrWhiteSpace(task.ActualChuteCode))
        {
            return task.ActualChuteCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(task.TargetChuteCode))
        {
            return task.TargetChuteCode.Trim();
        }

        return string.IsNullOrWhiteSpace(task.ResolvedDockCode) ? null : task.ResolvedDockCode.Trim();
    }

    /// <summary>
    /// 解析箱子追踪状态。
    /// </summary>
    /// <param name="log">扫描日志。</param>
    /// <param name="task">业务任务。</param>
    /// <returns>状态文案。</returns>
    private string ResolveStatus(ScanLogEntity log, BusinessTaskEntity? task)
    {
        if (!log.IsMatched)
        {
            return "Unmatched";
        }

        if (task is null)
        {
            return "Matched";
        }

        if (task.IsException || task.Status == BusinessTaskStatus.Exception)
        {
            return "ExceptionPending";
        }

        if (task.IsRecirculated || _queryPolicy.IsRecirculatedByResolvedDockCode(task.ResolvedDockCode))
        {
            return "RecirculationRescan";
        }

        return "Scanned";
    }

    /// <summary>
    /// 归一化可选文本。
    /// </summary>
    /// <param name="value">原始值。</param>
    /// <returns>归一化后的值。</returns>
    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

