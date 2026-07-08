using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义 IExportCatalogQueryService 类型。
/// </summary>
public interface IExportCatalogQueryService
{
    /// <summary>
    /// 执行 QueryAsync 方法。
    /// </summary>
    Task<ExportCatalogQueryResult> QueryAsync(ExportCatalogQueryRequest request, CancellationToken cancellationToken);
}

