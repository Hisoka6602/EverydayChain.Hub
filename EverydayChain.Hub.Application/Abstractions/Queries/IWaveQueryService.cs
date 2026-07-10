using EverydayChain.Hub.Application.Models;

namespace EverydayChain.Hub.Application.Abstractions.Queries;

/// <summary>
/// 定义 IWaveQueryService 类型。
/// </summary>
public interface IWaveQueryService
{
    /// <summary>
    /// 执行 QueryCurrentAsync 方法。
    /// </summary>
    Task<CurrentWaveQueryResult> QueryCurrentAsync(CurrentWaveQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 QueryOptionsAsync 方法。
    /// </summary>
    Task<WaveOptionsQueryResult> QueryOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 QuerySummaryAsync 方法。
    /// </summary>
    Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 QueryZonesAsync 方法。
    /// </summary>
    Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 ExportZonesCsvAsync 方法。
    /// </summary>
    Task<string> ExportZonesCsvAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 QueryListAsync 方法。
    /// </summary>
    Task<WaveListQueryResult> QueryListAsync(WaveListQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 ExportListCsvAsync 方法。
    /// </summary>
    Task<string> ExportListCsvAsync(WaveListQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 QueryCleanupWaveAsync 方法。
    /// </summary>
    Task<WaveCleanupQueryResult> QueryCleanupWaveAsync(string? waveCode, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 QueryDetailsAsync 方法。
    /// </summary>
    Task<WaveDetailQueryResult> QueryDetailsAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 执行 ExportDetailsCsvAsync 方法。
    /// </summary>
    Task<string> ExportDetailsCsvAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken);
}

