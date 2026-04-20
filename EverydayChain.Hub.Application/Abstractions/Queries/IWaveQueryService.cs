using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 波次查询服务抽象。
/// </summary>
public interface IWaveQueryService
{
    /// <summary>
    /// 查询时间区间内的波次选项。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次选项查询结果。</returns>
    Task<WaveOptionsQueryResult> QueryOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 查询单个波次摘要。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次摘要结果；未命中返回空值。</returns>
    Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 查询单个波次分区明细。
    /// </summary>
    /// <param name="request">查询请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>波次分区结果；未命中返回空值。</returns>
    Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken);
}
