using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// 定义当前类型。
/// </summary>
public sealed class ApiWarmupServiceTests
{
    [Fact]
    public async Task WarmupAsync_ShouldContinue_WhenSingleStepThrows()
    {
        var globalService = new ThrowingGlobalDashboardQueryService();
        var dockService = new RecordingDockDashboardQueryService();
        var waveService = new RecordingWaveQueryService();
        var repository = new InMemoryBusinessTaskRepository();
        var service = new ApiWarmupService(
            globalService,
            dockService,
            waveService,
            repository,
            NullLogger<ApiWarmupService>.Instance);

        await service.WarmupAsync(CancellationToken.None);

        Assert.Equal(1, dockService.QueryCount);
        Assert.Equal(1, waveService.OptionsQueryCount);
        Assert.Equal(1, waveService.SummaryQueryCount);
        Assert.Equal(1, waveService.ZonesQueryCount);
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class ThrowingGlobalDashboardQueryService : IGlobalDashboardQueryService
    {
        public Task<GlobalDashboardQueryResult> QueryAsync(GlobalDashboardQueryRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Warmup failure.");
        }
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class RecordingDockDashboardQueryService : IDockDashboardQueryService
    {
        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public int QueryCount { get; private set; }

        public Task<DockDashboardQueryResult> QueryAsync(DockDashboardQueryRequest request, CancellationToken cancellationToken)
        {
            QueryCount++;
            return Task.FromResult(new DockDashboardQueryResult());
        }
    }

    /// <summary>
    /// 定义当前类型。
    /// </summary>
    private sealed class RecordingWaveQueryService : IWaveQueryService
    {
        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public int CurrentQueryCount { get; private set; }

        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public int OptionsQueryCount { get; private set; }

        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public int SummaryQueryCount { get; private set; }

        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public int ZonesQueryCount { get; private set; }

        /// <summary>
        /// 获取或设置当前属性值。
        /// </summary>
        public int ListQueryCount { get; private set; }

        public Task<CurrentWaveQueryResult> QueryCurrentAsync(CurrentWaveQueryRequest request, CancellationToken cancellationToken)
        {
            CurrentQueryCount++;
            return Task.FromResult(new CurrentWaveQueryResult());
        }

        public Task<WaveOptionsQueryResult> QueryOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken)
        {
            OptionsQueryCount++;
            return Task.FromResult(new WaveOptionsQueryResult());
        }

        public Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken)
        {
            SummaryQueryCount++;
            return Task.FromResult<WaveSummaryQueryResult?>(null);
        }

        public Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
        {
            ZonesQueryCount++;
            return Task.FromResult<WaveZoneQueryResult?>(null);
        }

        public Task<string> ExportZonesCsvAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
        {
            ZonesQueryCount++;
            return Task.FromResult(string.Empty);
        }

        public Task<WaveListQueryResult> QueryListAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
        {
            ListQueryCount++;
            return Task.FromResult(new WaveListQueryResult());
        }

        public Task<string> ExportListCsvAsync(WaveListQueryRequest request, CancellationToken cancellationToken)
        {
            ListQueryCount++;
            return Task.FromResult(string.Empty);
        }

        public Task<WaveCleanupQueryResult> QueryCleanupWaveAsync(string waveCode, CancellationToken cancellationToken)
        {
            return Task.FromResult(new WaveCleanupQueryResult());
        }

        public Task<WaveDetailQueryResult> QueryDetailsAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new WaveDetailQueryResult());
        }

        public Task<string> ExportDetailsCsvAsync(WaveDetailQueryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Empty);
        }
    }
}

