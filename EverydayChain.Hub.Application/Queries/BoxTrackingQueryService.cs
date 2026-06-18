using EverydayChain.Hub.Application.Abstractions.Persistence;
using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Aggregates.BusinessTaskAggregate;
using EverydayChain.Hub.Domain.Aggregates.ScanLogAggregate;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Application.Queries;

public sealed class BoxTrackingQueryService(
    IScanLogRepository scanLogRepository,
    IBusinessTaskRepository businessTaskRepository) : IBoxTrackingQueryService
{
    private readonly BusinessTaskQueryPolicy _queryPolicy = new();

    public async Task<BoxTrackingQueryResult> QueryAsync(BoxTrackingQueryRequest request, CancellationToken cancellationToken)
    {
        var result = new BoxTrackingQueryResult
        {
            StartTimeLocal = request.StartTimeLocal,
            EndTimeLocal = request.EndTimeLocal,
            PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
            PageSize = request.PageSize <= 0 ? 50 : request.PageSize
        };
        if (request.EndTimeLocal <= request.StartTimeLocal)
        {
            return result;
        }

        var skip = (result.PageNumber - 1) * result.PageSize;
        var page = await scanLogRepository.QueryPageAsync(
            request.StartTimeLocal,
            request.EndTimeLocal,
            request.BoxId,
            request.Scanner,
            skip,
            result.PageSize,
            cancellationToken);

        var taskIds = page.Items
            .Where(item => item.BusinessTaskId.HasValue)
            .Select(item => item.BusinessTaskId!.Value)
            .Distinct()
            .ToArray();
        var taskMap = await businessTaskRepository.GetByIdsAsync(taskIds, cancellationToken);

        var items = page.Items
            .Select(log => MapItem(log, ResolveTask(log, taskMap), request.ChuteCode))
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();

        result.TotalCount = string.IsNullOrWhiteSpace(request.ChuteCode)
            ? page.TotalCount
            : items.Count + skip;
        result.Items = items;
        return result;
    }

    private BoxTrackingItem? MapItem(
        ScanLogEntity log,
        BusinessTaskEntity? task,
        string? requestedChuteCode)
    {
        var chute = task is null ? null : ResolveChute(task);
        if (!string.IsNullOrWhiteSpace(requestedChuteCode)
            && !string.Equals(chute, requestedChuteCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

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

    private static BusinessTaskEntity? ResolveTask(
        ScanLogEntity log,
        IReadOnlyDictionary<long, BusinessTaskEntity> taskMap)
    {
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
}
