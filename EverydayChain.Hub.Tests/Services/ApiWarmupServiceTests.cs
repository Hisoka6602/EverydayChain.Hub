using EverydayChain.Hub.Application.Abstractions.Queries;
using EverydayChain.Hub.Application.Models;
using EverydayChain.Hub.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EverydayChain.Hub.Tests.Services;

/// <summary>
/// API 启动预热服务测试。
/// </summary>
public sealed class ApiWarmupServiceTests
{
    /// <summary>
    /// 任一步骤失败时应记录并继续执行后续步骤。
    /// </summary>
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
    /// 抛出异常的总看板查询服务替身。
    /// </summary>
    private sealed class ThrowingGlobalDashboardQueryService : IGlobalDashboardQueryService
    {
        /// <inheritdoc/>
        public Task<GlobalDashboardQueryResult> QueryAsync(GlobalDashboardQueryRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("测试桩：总看板预热失败。");
        }
    }

    /// <summary>
    /// 记录调用次数的码头看板查询服务替身。
    /// </summary>
    private sealed class RecordingDockDashboardQueryService : IDockDashboardQueryService
    {
        /// <summary>
        /// 调用次数。
        /// </summary>
        public int QueryCount { get; private set; }

        /// <inheritdoc/>
        public Task<DockDashboardQueryResult> QueryAsync(DockDashboardQueryRequest request, CancellationToken cancellationToken)
        {
            QueryCount++;
            return Task.FromResult(new DockDashboardQueryResult());
        }
    }

    /// <summary>
    /// 记录调用次数的波次查询服务替身。
    /// </summary>
    private sealed class RecordingWaveQueryService : IWaveQueryService
    {
        /// <summary>
        /// 波次选项调用次数。
        /// </summary>
        public int OptionsQueryCount { get; private set; }

        /// <summary>
        /// 波次摘要调用次数。
        /// </summary>
        public int SummaryQueryCount { get; private set; }

        /// <summary>
        /// 波次分区调用次数。
        /// </summary>
        public int ZonesQueryCount { get; private set; }

        /// <inheritdoc/>
        public Task<WaveOptionsQueryResult> QueryOptionsAsync(WaveOptionsQueryRequest request, CancellationToken cancellationToken)
        {
            OptionsQueryCount++;
            return Task.FromResult(new WaveOptionsQueryResult());
        }

        /// <inheritdoc/>
        public Task<WaveSummaryQueryResult?> QuerySummaryAsync(WaveSummaryQueryRequest request, CancellationToken cancellationToken)
        {
            SummaryQueryCount++;
            return Task.FromResult<WaveSummaryQueryResult?>(null);
        }

        /// <inheritdoc/>
        public Task<WaveZoneQueryResult?> QueryZonesAsync(WaveZoneQueryRequest request, CancellationToken cancellationToken)
        {
            ZonesQueryCount++;
            return Task.FromResult<WaveZoneQueryResult?>(null);
        }
    }
}
