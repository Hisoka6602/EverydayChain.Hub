using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Domain.Enums;

namespace EverydayChain.Hub.Tests.Host.Controllers;

/// <summary>
/// 定义 StubBusinessTaskReadService 类型。
/// </summary>
internal sealed class StubBusinessTaskReadService : IBusinessTaskReadService
{
    /// <summary>
    /// 获取或设置 LastRequest。
    /// </summary>
    public BusinessTaskQueryRequest? LastRequest { get; private set; }

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
            OrderId = "ORDER-1",
            StoreId = "STORE-1",
            StoreName = "Store Name 1",
            ProductCode = "SKU-1",
            PickLocation = "A-01-01",
            SourceType = BusinessTaskSourceType.Split,
            Status = BusinessTaskStatus.Created,
            DockCode = "7",
            CreatedTimeLocal = DateTime.SpecifyKind(new DateTime(2026, 4, 17, 1, 0, 0), DateTimeKind.Local)
        }]
    };

    public Task<BusinessTaskQueryResult> QueryTasksAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }

    public Task<BusinessTaskQueryResult> QueryExceptionsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }

    public Task<BusinessTaskQueryResult> QueryRecirculationsAsync(BusinessTaskQueryRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Result);
    }
}

