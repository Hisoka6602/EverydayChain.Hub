using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IExportCatalogQueryService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<ExportCatalogQueryResult> QueryAsync(ExportCatalogQueryRequest request, CancellationToken cancellationToken);
}

