using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义当前类型。
/// </summary>
public interface IWaveQueryService
{
    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<CurrentWaveQueryResult> QueryCurrentAsync(CurrentWaveQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<WaveOptionsQueryResult> QueryOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<string> ExportZonesCsvAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<WaveListQueryResult> QueryListAsync(WaveListQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<string> ExportListCsvAsync(WaveListQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<WaveCleanupQueryResult> QueryCleanupWaveAsync(string waveCode, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<WaveDetailQueryResult> QueryDetailsAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行当前方法。
    /// </summary>
    Task<string> ExportDetailsCsvAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken);
}

