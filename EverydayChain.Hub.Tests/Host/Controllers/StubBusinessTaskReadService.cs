using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 业务任务查询服务替身。
/// </summary>
internal sealed class StubBusinessTaskReadService : IBusinessTaskReadService
{
    /// <summary>
    /// 最近一次查询请求。
    /// </summary>
    public BusinessTaskQueryRequest? LastRequest { get; private set; }

    /// <summary>
    /// 固定查询结果。
    /// </summary>
    public BusinessTaskQueryResult Result { get; set; } = new()
    {
        TotalCount = 1,
        PageNumber = 1,
        PageSize = 50,
        HasMore = true,
        NextLastCreatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 2, 0, 0), DateTimeKind.Local),
        NextLastId = 1001,
        PaginationMode = "Cursor",
        Items = [new BusinessTaskQueryItem
        {
            TaskCode = "T1",
            SourceType = BusinessTaskSourceType.Split,
            Status = BusinessTaskStatus.Created,
            DockCode = "7",
            CreatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 1, 0, 0), DateTimeKind.Local)
        }]
    };

    /// <inheritdoc/>
    public Task<BusinessTaskQueryResult> QueryTasksAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }

    /// <inheritdoc/>
    public Task<BusinessTaskQueryResult> QueryExceptionsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }

    /// <inheritdoc/>
    public Task<BusinessTaskQueryResult> QueryRecirculationsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }
}
